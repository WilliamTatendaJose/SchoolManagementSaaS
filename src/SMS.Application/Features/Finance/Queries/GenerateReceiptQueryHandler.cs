using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Branding;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Finance.Queries;

public class GenerateReceiptQueryHandler : IRequestHandler<GenerateReceiptQuery, Result<ReceiptFileDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IReceiptGenerator _generator;
    private readonly ICurrentUserService _currentUser;

    public GenerateReceiptQueryHandler(
        IApplicationDbContext context,
        IReceiptGenerator generator,
        ICurrentUserService currentUser)
    {
        _context = context;
        _generator = generator;
        _currentUser = currentUser;
    }

    public async Task<Result<ReceiptFileDto>> Handle(GenerateReceiptQuery request, CancellationToken cancellationToken)
    {
        var payment = await _context.Payments
            .AsNoTracking()
            .Include(p => p.Invoice)
                .ThenInclude(i => i.Student)
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken);

        if (payment == null)
        {
            return Result<ReceiptFileDto>.Failure("Payment not found");
        }

        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == _currentUser.TenantId, cancellationToken);
        var invoice = payment.Invoice;
        var balance = invoice.TotalAmount - invoice.DiscountAmount - invoice.PaidAmount;

        var model = new ReceiptModel
        {
            Branding = BrandingBuilder.From(tenant),
            ReceiptNumber = payment.ReceiptNumber,
            StudentName = invoice.Student.FullName,
            StudentNumber = invoice.Student.StudentNumber,
            InvoiceNumber = invoice.InvoiceNumber,
            Amount = payment.Amount,
            Currency = payment.Currency,
            PaymentMethod = payment.PaymentMethod.ToString(),
            PaymentDate = payment.PaymentDate,
            RemainingBalance = balance
        };

        var pdf = _generator.Generate(model);
        var safeReceipt = new string(payment.ReceiptNumber.Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '_').ToArray());

        return Result<ReceiptFileDto>.Success(new ReceiptFileDto
        {
            FileName = $"Receipt_{safeReceipt}.pdf",
            Content = pdf
        });
    }
}
