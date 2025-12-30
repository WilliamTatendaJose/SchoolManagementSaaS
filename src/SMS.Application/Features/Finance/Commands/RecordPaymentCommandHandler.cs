using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;
using SMS.Domain.Enums;

namespace SMS.Application.Features.Finance.Commands;

public class RecordPaymentCommandHandler : IRequestHandler<RecordPaymentCommand, Result<PaymentResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public RecordPaymentCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<PaymentResultDto>> Handle(RecordPaymentCommand request, CancellationToken cancellationToken)
    {
        var invoice = await _context.Invoices.FindAsync([request.InvoiceId], cancellationToken);
        
        if (invoice == null)
        {
            return Result<PaymentResultDto>.Failure("Invoice not found");
        }

        var currentBalance = invoice.TotalAmount - invoice.DiscountAmount - invoice.PaidAmount;
        
        if (request.Amount <= 0)
        {
            return Result<PaymentResultDto>.Failure("Payment amount must be greater than zero");
        }

        var receiptNumber = await GenerateReceiptNumberAsync(cancellationToken);

        var payment = new Payment
        {
            ReceiptNumber = receiptNumber,
            InvoiceId = request.InvoiceId,
            Amount = request.Amount,
            PaymentMethod = Enum.Parse<PaymentMethod>(request.PaymentMethod),
            Status = PaymentStatus.Completed,
            PaymentDate = request.PaymentDate ?? DateTime.UtcNow,
            TransactionReference = request.TransactionReference,
            MobileMoneyNumber = request.MobileMoneyNumber,
            BankName = request.BankName,
            Notes = request.Notes,
            ReceivedById = _currentUserService.UserId
        };

        _context.Payments.Add(payment);
        
        // Update invoice paid amount
        invoice.PaidAmount += request.Amount;
        
        await _context.SaveChangesAsync(cancellationToken);

        var remainingBalance = invoice.TotalAmount - invoice.DiscountAmount - invoice.PaidAmount;

        return Result<PaymentResultDto>.Success(new PaymentResultDto
        {
            PaymentId = payment.Id,
            ReceiptNumber = receiptNumber,
            RemainingBalance = remainingBalance
        });
    }

    private async Task<string> GenerateReceiptNumberAsync(CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var count = await _context.Payments
            .CountAsync(p => p.PaymentDate.Year == year, cancellationToken) + 1;

        return $"RCP-{year}-{count:D6}";
    }
}
