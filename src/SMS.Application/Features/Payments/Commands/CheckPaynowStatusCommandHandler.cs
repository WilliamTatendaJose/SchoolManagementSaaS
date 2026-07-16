using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Payments.Commands;

public class CheckPaynowStatusCommandHandler : IRequestHandler<CheckPaynowStatusCommand, Result<PaymentSettlementDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPaymentGatewayService _gateway;

    public CheckPaynowStatusCommandHandler(IApplicationDbContext context, IPaymentGatewayService gateway)
    {
        _context = context;
        _gateway = gateway;
    }

    public async Task<Result<PaymentSettlementDto>> Handle(CheckPaynowStatusCommand request, CancellationToken cancellationToken)
    {
        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken);

        if (payment == null)
        {
            return Result<PaymentSettlementDto>.Failure("Payment not found");
        }

        if (string.IsNullOrWhiteSpace(payment.GatewayPollUrl))
        {
            return Result<PaymentSettlementDto>.Failure("Payment has no gateway poll URL");
        }

        var status = await _gateway.CheckStatusAsync(payment.GatewayPollUrl, cancellationToken);
        if (!status.IsValid)
        {
            return Result<PaymentSettlementDto>.Failure(status.Error ?? "Could not verify payment status");
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
