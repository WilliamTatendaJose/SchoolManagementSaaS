using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;

namespace SMS.Application.Features.Fees.Commands;

public class GenerateInvoicesCommandHandler : IRequestHandler<GenerateInvoicesCommand, Result<InvoiceGenerationResultDto>>
{
    private readonly IApplicationDbContext _context;

    public GenerateInvoicesCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<InvoiceGenerationResultDto>> Handle(GenerateInvoicesCommand request, CancellationToken cancellationToken)
    {
        var term = await _context.AcademicTerms
            .FirstOrDefaultAsync(t => t.Id == request.AcademicTermId, cancellationToken);

        if (term == null)
        {
            return Result<InvoiceGenerationResultDto>.Failure("Academic term not found");
        }

        var currency = await _context.Tenants
            .Where(t => t.Id == term.TenantId)
            .Select(t => t.Currency)
            .FirstOrDefaultAsync(cancellationToken) ?? "USD";

        // Active enrollments in the term's academic year, optionally scoped to one class
        var enrollmentQuery = _context.Enrollments
            .Where(e => e.IsActive && e.AcademicYearId == term.AcademicYearId);

        if (request.ClassId.HasValue)
        {
            enrollmentQuery = enrollmentQuery.Where(e => e.ClassId == request.ClassId.Value);
        }

        var enrollments = await enrollmentQuery
            .Select(e => new { e.StudentId, e.ClassId })
            .ToListAsync(cancellationToken);

        if (enrollments.Count == 0)
        {
            return Result<InvoiceGenerationResultDto>.Failure("No active enrollments found for the selected term/class");
        }

        var cohortIds = enrollments.Select(e => e.StudentId).ToHashSet();

        // Fee structures for the year, grouped by class, honouring the optional-fee flag
        var feeStructureQuery = _context.FeeStructures
            .Where(f => f.AcademicYearId == term.AcademicYearId);

        if (request.ClassId.HasValue)
        {
            feeStructureQuery = feeStructureQuery.Where(f => f.ClassId == request.ClassId.Value);
        }

        if (!request.IncludeOptionalFees)
        {
            feeStructureQuery = feeStructureQuery.Where(f => !f.IsOptional);
        }

        var feesByClass = (await feeStructureQuery.ToListAsync(cancellationToken))
            .GroupBy(f => f.ClassId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Students already invoiced for this term (idempotency)
        var alreadyInvoiced = (await _context.Invoices
            .Where(i => i.AcademicTermId == request.AcademicTermId)
            .Select(i => i.StudentId)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        var siblingDiscount = await BuildSiblingDiscountEvaluatorAsync(request, cohortIds, cancellationToken);
        var arrearsByStudent = await BuildArrearsMapAsync(request, cohortIds, cancellationToken);

        var year = DateTime.UtcNow.Year;
        var invoiceSequence = await _context.Invoices
            .CountAsync(i => i.InvoiceDate.Year == year, cancellationToken);

        var invoicesCreated = 0;
        var studentsSkipped = 0;
        var studentsWithoutFees = 0;
        decimal totalBilled = 0m;
        decimal totalDiscount = 0m;
        decimal totalArrears = 0m;

        // One invoice per student even if (defensively) multiple enrollments exist
        var processedStudents = new HashSet<Guid>();

        foreach (var enrollment in enrollments)
        {
            if (!processedStudents.Add(enrollment.StudentId))
            {
                continue;
            }

            if (alreadyInvoiced.Contains(enrollment.StudentId))
            {
                studentsSkipped++;
                continue;
            }

            if (!feesByClass.TryGetValue(enrollment.ClassId, out var fees) || fees.Count == 0)
            {
                studentsWithoutFees++;
                continue;
            }

            var feeSubtotal = fees.Sum(f => f.Amount);
            var discount = siblingDiscount(enrollment.StudentId)
                ? Math.Round(feeSubtotal * request.SiblingDiscountPercent / 100m, 2)
                : 0m;
            var arrears = arrearsByStudent.GetValueOrDefault(enrollment.StudentId, 0m);

            invoiceSequence++;
            var invoice = new Invoice
            {
                InvoiceNumber = $"INV-{year}-{invoiceSequence:D6}",
                StudentId = enrollment.StudentId,
                AcademicTermId = request.AcademicTermId,
                InvoiceDate = DateTime.UtcNow,
                DueDate = request.DueDate,
                TotalAmount = feeSubtotal + arrears,
                DiscountAmount = discount,
                Currency = currency
            };

            foreach (var fee in fees)
            {
                invoice.Items.Add(new InvoiceItem
                {
                    FeeStructureId = fee.Id,
                    Description = fee.Name,
                    Amount = fee.Amount,
                    Quantity = 1
                });
            }

            if (arrears > 0)
            {
                invoice.Items.Add(new InvoiceItem
                {
                    Description = "Arrears brought forward",
                    Amount = arrears,
                    Quantity = 1
                });
            }

            _context.Invoices.Add(invoice);
            invoicesCreated++;
            totalBilled += feeSubtotal;
            totalDiscount += discount;
            totalArrears += arrears;
        }

        if (invoicesCreated > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return Result<InvoiceGenerationResultDto>.Success(new InvoiceGenerationResultDto
        {
            InvoicesCreated = invoicesCreated,
            StudentsSkipped = studentsSkipped,
            StudentsWithoutFees = studentsWithoutFees,
            TotalBilled = totalBilled,
            TotalDiscount = totalDiscount,
            TotalArrearsCarriedForward = totalArrears
        });
    }

    /// <summary>
    /// Returns a predicate telling whether a student should receive the sibling discount:
    /// true when the student shares a guardian with at least one older student in the run
    /// (the eldest sibling pays full fees).
    /// </summary>
    private async Task<Func<Guid, bool>> BuildSiblingDiscountEvaluatorAsync(
        GenerateInvoicesCommand request, HashSet<Guid> cohortIds, CancellationToken cancellationToken)
    {
        if (request.SiblingDiscountPercent <= 0)
        {
            return _ => false;
        }

        var dobByStudent = await _context.Students
            .Where(s => cohortIds.Contains(s.Id))
            .Select(s => new { s.Id, s.DateOfBirth })
            .ToDictionaryAsync(s => s.Id, s => s.DateOfBirth, cancellationToken);

        var links = await _context.StudentGuardians
            .Where(sg => cohortIds.Contains(sg.StudentId))
            .Select(sg => new { sg.StudentId, sg.GuardianId })
            .ToListAsync(cancellationToken);

        var guardiansByStudent = links
            .GroupBy(l => l.StudentId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.GuardianId).ToHashSet());

        var studentsByGuardian = links
            .GroupBy(l => l.GuardianId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.StudentId).ToList());

        return studentId =>
        {
            if (!guardiansByStudent.TryGetValue(studentId, out var guardianIds))
            {
                return false;
            }

            var siblingSet = new HashSet<Guid>();
            foreach (var guardianId in guardianIds)
            {
                foreach (var sibling in studentsByGuardian[guardianId])
                {
                    siblingSet.Add(sibling);
                }
            }

            if (siblingSet.Count <= 1)
            {
                return false;
            }

            var eldest = siblingSet
                .OrderBy(id => dobByStudent[id])
                .ThenBy(id => id)
                .First();

            return studentId != eldest;
        };
    }

    private async Task<Dictionary<Guid, decimal>> BuildArrearsMapAsync(
        GenerateInvoicesCommand request, HashSet<Guid> cohortIds, CancellationToken cancellationToken)
    {
        if (!request.CarryForwardArrears)
        {
            return [];
        }

        var invoices = await _context.Invoices
            .Where(i => cohortIds.Contains(i.StudentId))
            .Select(i => new { i.StudentId, i.TotalAmount, i.DiscountAmount, i.PaidAmount })
            .ToListAsync(cancellationToken);

        return invoices
            .GroupBy(i => i.StudentId)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(x =>
                {
                    var balance = x.TotalAmount - x.DiscountAmount - x.PaidAmount;
                    return balance > 0 ? balance : 0m;
                }));
    }
}
