using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Payments.Commands;

/// <summary>
/// Polls the gateway for a payment's current status and settles the invoice if paid.
/// </summary>
public record CheckPaynowStatusCommand(Guid PaymentId) : IRequest<Result<PaymentSettlementDto>>;

public record PaymentSettlementDto
{
    public Guid PaymentId { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool Settled { get; init; }
    public decimal InvoiceBalance { get; init; }
}
