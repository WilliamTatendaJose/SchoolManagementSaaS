using MediatR;
using SMS.Application.Common.Models;
using SMS.Application.Features.Payments.Commands;

namespace SMS.Application.Features.ParentPortal.Commands;

/// <summary>
/// Starts an online (Paynow) payment against one of the signed-in parent's children's
/// invoices. Ownership is checked against the invoice's student before delegating to the
/// shared initiation logic - a parent can only ever pay their own child's fees.
/// </summary>
public record InitiateMyChildOnlinePaymentCommand : IRequest<Result<OnlinePaymentInitiationDto>>
{
    public Guid InvoiceId { get; init; }
    /// <summary>Amount to pay; defaults to the invoice's outstanding balance.</summary>
    public decimal? Amount { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
}
