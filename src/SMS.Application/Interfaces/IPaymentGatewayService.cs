namespace SMS.Application.Interfaces;

/// <summary>
/// Abstraction over an online payment gateway (e.g. Paynow for the Zimbabwe market).
/// </summary>
public interface IPaymentGatewayService
{
    /// <summary>
    /// Starts a transaction with the gateway and returns the redirect/poll details.
    /// </summary>
    Task<PaymentInitiationResult> InitiatePaymentAsync(PaymentInitiationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries the gateway for the current status of a transaction via its poll URL.
    /// </summary>
    Task<PaymentStatusResult> CheckStatusAsync(string pollUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Parses and verifies an asynchronous status callback posted by the gateway.
    /// The fields must be in the order the gateway sent them so the hash verifies.
    /// </summary>
    PaymentStatusResult ParseStatusCallback(IReadOnlyList<KeyValuePair<string, string>> fields);
}

public record PaymentInitiationRequest
{
    public required string Reference { get; init; }
    public required decimal Amount { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string ItemDescription { get; init; } = "School fees";

    /// <summary>
    /// Tenant the payment belongs to; appended to the configured result URL base so the
    /// unauthenticated gateway callback can be scoped to the correct tenant.
    /// </summary>
    public Guid? TenantId { get; init; }
}

public record PaymentInitiationResult
{
    public bool Success { get; init; }
    public string? RedirectUrl { get; init; }
    public string? PollUrl { get; init; }
    public string? Instructions { get; init; }
    public string? Error { get; init; }
}

/// <summary>
/// Gateway-neutral transaction states (Paynow's vocabulary maps onto these).
/// </summary>
public enum GatewayPaymentStatus
{
    Created,
    Sent,
    Paid,
    AwaitingDelivery,
    Delivered,
    Cancelled,
    Failed,
    Refunded
}

public record PaymentStatusResult
{
    /// <summary>True when the gateway signature/hash verified successfully.</summary>
    public bool IsValid { get; init; }
    public string? Reference { get; init; }
    public string? GatewayReference { get; init; }
    public decimal Amount { get; init; }
    public GatewayPaymentStatus Status { get; init; }
    public string? Error { get; init; }

    public bool IsPaid => Status is GatewayPaymentStatus.Paid
        or GatewayPaymentStatus.AwaitingDelivery
        or GatewayPaymentStatus.Delivered;
}
