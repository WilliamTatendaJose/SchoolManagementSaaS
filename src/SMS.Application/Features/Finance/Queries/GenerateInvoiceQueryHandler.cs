using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Branding;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Finance.Queries;

public class GenerateInvoiceQueryHandler : IRequestHandler<GenerateInvoiceQuery, Result<InvoiceFileDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IInvoiceGenerator _generator;
    private readonly ICurrentUserService _currentUser;

    public GenerateInvoiceQueryHandler(IApplicationDbContext context, IInvoiceGenerator generator, ICurrentUserService currentUser)
    {
        _context = context;
        _generator = generator;
        _currentUser = currentUser;
    }

    public async Task<Result<InvoiceFileDto>> Handle(GenerateInvoiceQuery request, CancellationToken cancellationToken)
    {
        var invoice = await _context.Invoices
            .AsNoTracking()
            .Include(i => i.Student).ThenInclude(s => s.CurrentClass)
            .Include(i => i.AcademicTerm)
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken);

        if (invoice == null)
        {
            return Result<InvoiceFileDto>.Failure("Invoice not found");
        }

        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == _currentUser.TenantId, cancellationToken);

        var lines = invoice.Items
            .Select(item => new InvoiceLine
            {
                Description = item.Description,
                Quantity = item.Quantity,
                Amount = item.Amount,
                LineTotal = item.Amount * item.Quantity
            })
            .ToList();

        // Bulk invoicing carries prior-term arrears into TotalAmount without a matching
        // line item, so the line items alone can under-count the total. Surface the gap as
        // an explicit line so the printed subtotal reconciles to the invoice total.
        var lineSum = lines.Sum(l => l.LineTotal);
        var arrears = invoice.TotalAmount - lineSum;
        if (arrears > 0)
        {
            lines.Add(new InvoiceLine
            {
                Description = "Balance brought forward",
                Quantity = 1,
                Amount = arrears,
                LineTotal = arrears
            });
        }

        var subtotal = invoice.TotalAmount;

        var model = new InvoiceModel
        {
            Branding = BrandingBuilder.From(tenant),
            InvoiceNumber = invoice.InvoiceNumber,
            StudentName = invoice.Student.FullName,
            StudentNumber = invoice.Student.StudentNumber,
            ClassName = invoice.Student.CurrentClass?.Name ?? string.Empty,
            TermName = invoice.AcademicTerm.Name,
            Currency = invoice.Currency,
            InvoiceDate = invoice.InvoiceDate,
            DueDate = invoice.DueDate,
            Lines = lines,
            Subtotal = subtotal,
            Discount = invoice.DiscountAmount,
            Paid = invoice.PaidAmount,
            Balance = invoice.TotalAmount - invoice.DiscountAmount - invoice.PaidAmount,
            Notes = invoice.Notes
        };

        var pdf = _generator.Generate(model);
        var safe = new string(invoice.InvoiceNumber.Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '_').ToArray());

        return Result<InvoiceFileDto>.Success(new InvoiceFileDto
        {
            FileName = $"Invoice_{safe}.pdf",
            Content = pdf
        });
    }
}
