using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;

namespace SMS.Application.Features.Finance.Commands;

public class CreateInvoiceCommandHandler : IRequestHandler<CreateInvoiceCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateInvoiceCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateInvoiceCommand request, CancellationToken cancellationToken)
    {
        // Verify student exists
        var student = await _context.Students.FindAsync([request.StudentId], cancellationToken);
        if (student == null)
        {
            return Result<Guid>.Failure("Student not found");
        }

        // Verify term exists
        var term = await _context.AcademicTerms.FindAsync([request.AcademicTermId], cancellationToken);
        if (term == null)
        {
            return Result<Guid>.Failure("Academic term not found");
        }

        // Generate invoice number
        var invoiceNumber = await GenerateInvoiceNumberAsync(cancellationToken);

        var invoice = new Invoice
        {
            InvoiceNumber = invoiceNumber,
            StudentId = request.StudentId,
            AcademicTermId = request.AcademicTermId,
            InvoiceDate = DateTime.UtcNow,
            DueDate = request.DueDate,
            TotalAmount = request.Items.Sum(i => i.Amount * i.Quantity),
            DiscountAmount = request.DiscountAmount ?? 0,
            Notes = request.Notes
        };

        foreach (var item in request.Items)
        {
            invoice.Items.Add(new InvoiceItem
            {
                FeeStructureId = item.FeeStructureId,
                Description = item.Description,
                Amount = item.Amount,
                Quantity = item.Quantity
            });
        }

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(invoice.Id);
    }

    private async Task<string> GenerateInvoiceNumberAsync(CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var count = await _context.Invoices
            .CountAsync(i => i.InvoiceDate.Year == year, cancellationToken) + 1;

        return $"INV-{year}-{count:D6}";
    }
}
