using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;
using SMS.Domain.Enums;

namespace SMS.Application.Features.Payments.Commands;

public class InitiateOnlinePaymentCommandHandler : IRequestHandler<InitiateOnlinePaymentCommand, Result<OnlinePaymentInitiationDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPaymentGatewayService _gateway;
    private readonly ICurrentUserService _currentUser;

    public InitiateOnlinePaymentCommandHandler(
        IApplicationDbContext context,
        IPaymentGatewayService gateway,
        ICurrentUserService currentUser)
    {
        _context = context;
        _gateway = gateway;
        _currentUser = currentUser;
    }

    public async Task<Result<OnlinePaymentInitiationDto>> Handle(InitiateOnlinePaymentCommand request, CancellationToken cancellationToken)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Student)
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken);

        if (invoice == null)
        {
            return Result<OnlinePaymentInitiationDto>.Failure("Invoice not found");
        }

        var balance = invoice.TotalAmount - invoice.DiscountAmount - invoice.PaidAmount;
        var amount = request.Amount ?? balance;

        if (amount <= 0)
        {
            return Result<OnlinePaymentInitiationDto>.Failure("Payment amount must be greater than zero");
        }

        if (amount > balance)
        {
            return Result<OnlinePaymentInitiationDto>.Failure("Payment amount exceeds the invoice balance");
        }

        var reference = $"PAY-{invoice.InvoiceNumber}-{Guid.NewGuid():N}"[..40];

        var payment = new Payment
        {
            ReceiptNumber = reference,
            InvoiceId = invoice.Id,
            Amount = amount,
            PaymentMethod = PaymentMethod.MobileMoney,
            Status = PaymentStatus.Pending,
            PaymentDate = DateTime.UtcNow,
            TransactionReference = reference,
            MobileMoneyNumber = request.Phone
        };

        var initiation = await _gateway.InitiatePaymentAsync(new PaymentInitiationRequest
        {
            Reference = reference,
            Amount = amount,
            Email = request.Email,
            Phone = request.Phone,
            ItemDescription = $"Fees for {invoice.Student.FullName} ({invoice.InvoiceNumber})",
            TenantId = _currentUser.TenantId
        }, cancellationToken);

        if (!initiation.Success)
        {
            return Result<OnlinePaymentInitiationDto>.Failure(initiation.Error ?? "Could not start the online payment");
        }

        payment.GatewayPollUrl = initiation.PollUrl;
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<OnlinePaymentInitiationDto>.Success(new OnlinePaymentInitiationDto
        {
            PaymentId = payment.Id,
            Reference = reference,
            RedirectUrl = initiation.RedirectUrl,
            PollUrl = initiation.PollUrl,
            Instructions = initiation.Instructions
        });
    }
}
