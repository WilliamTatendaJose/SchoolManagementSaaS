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

        var year = DateTime.UtcNow.Year;
        var invoiceSequence = await _context.Invoices
            .CountAsync(i => i.InvoiceDate.Year == year, cancellationToken);

        var invoicesCreated = 0;
        var studentsSkipped = 0;
        var studentsWithoutFees = 0;
        decimal totalBilled = 0m;

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

            invoiceSequence++;
            var invoice = new Invoice
            {
                InvoiceNumber = $"INV-{year}-{invoiceSequence:D6}",
                StudentId = enrollment.StudentId,
                AcademicTermId = request.AcademicTermId,
                InvoiceDate = DateTime.UtcNow,
                DueDate = request.DueDate,
                TotalAmount = fees.Sum(f => f.Amount),
                DiscountAmount = 0
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

            _context.Invoices.Add(invoice);
            invoicesCreated++;
            totalBilled += invoice.TotalAmount;
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
            TotalBilled = totalBilled
        });
    }
}
