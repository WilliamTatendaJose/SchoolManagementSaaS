using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Finance.Commands;

public record RecordPaymentCommand : IRequest<Result<PaymentResultDto>>
{
    public Guid InvoiceId { get; init; }
    public decimal Amount { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
    public DateTime? PaymentDate { get; init; }
    public string? TransactionReference { get; init; }
    public string? MobileMoneyNumber { get; init; }
    public string? BankName { get; init; }
    public string? Notes { get; init; }
}

public record PaymentResultDto
{
    public Guid PaymentId { get; init; }
    public string ReceiptNumber { get; init; } = string.Empty;
    public decimal RemainingBalance { get; init; }
}
