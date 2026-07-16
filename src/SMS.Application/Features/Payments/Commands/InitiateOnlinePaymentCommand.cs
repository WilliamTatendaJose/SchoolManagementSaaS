using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Payments.Commands;

/// <summary>
/// Starts an online (Paynow) payment against an invoice and returns the redirect/poll URLs.
/// </summary>
public record InitiateOnlinePaymentCommand : IRequest<Result<OnlinePaymentInitiationDto>>
{
    public Guid InvoiceId { get; init; }

    /// <summary>Amount to pay; defaults to the invoice's outstanding balance.</summary>
    public decimal? Amount { get; init; }

    public string? Email { get; init; }
    public string? Phone { get; init; }
}

public record OnlinePaymentInitiationDto
{
    public Guid PaymentId { get; init; }
    public string Reference { get; init; } = string.Empty;
    public string? RedirectUrl { get; init; }
    public string? PollUrl { get; init; }
    public string? Instructions { get; init; }
}
