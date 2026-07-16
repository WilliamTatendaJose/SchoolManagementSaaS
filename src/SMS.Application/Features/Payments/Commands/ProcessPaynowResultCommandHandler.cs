using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Payments.Commands;

public class ProcessPaynowResultCommandHandler : IRequestHandler<ProcessPaynowResultCommand, Result<PaymentSettlementDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPaymentGatewayService _gateway;

    public ProcessPaynowResultCommandHandler(IApplicationDbContext context, IPaymentGatewayService gateway)
    {
        _context = context;
        _gateway = gateway;
    }

    public async Task<Result<PaymentSettlementDto>> Handle(ProcessPaynowResultCommand request, CancellationToken cancellationToken)
    {
        var status = _gateway.ParseStatusCallback(request.Fields);
        if (!status.IsValid)
        {
            return Result<PaymentSettlementDto>.Failure(status.Error ?? "Invalid callback");
        }

        if (string.IsNullOrWhiteSpace(status.Reference))
        {
            return Result<PaymentSettlementDto>.Failure("Callback is missing the payment reference");
        }

        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.TransactionReference == status.Reference, cancellationToken);

        if (payment == null)
        {
            return Result<PaymentSettlementDto>.Failure("Payment not found for reference");
        }

        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == payment.InvoiceId, cancellationToken);

        if (invoice == null)
        {
            return Result<PaymentSettlementDto>.Failure("Invoice not found");
        }

        var settled = PaymentSettlement.Apply(payment, invoice, status);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<PaymentSettlementDto>.Success(new PaymentSettlementDto
        {
            PaymentId = payment.Id,
            Status = payment.Status.ToString(),
            Settled = settled,
            InvoiceBalance = invoice.TotalAmount - invoice.DiscountAmount - invoice.PaidAmount
        });
    }
}
