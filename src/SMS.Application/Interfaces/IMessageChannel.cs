namespace SMS.Application.Interfaces;

/// <summary>
/// A delivery channel for outbound messages (SMS, WhatsApp, ...). Implementations live in
/// the Infrastructure layer and are selected by <see cref="Channel"/> at dispatch time.
/// </summary>
public interface IMessageChannel
{
    /// <summary>Channel identifier, e.g. "SMS" or "WhatsApp".</summary>
    string Channel { get; }

    Task<MessageDeliveryResult> SendAsync(string recipient, string content, CancellationToken cancellationToken = default);
}

public record MessageDeliveryResult
{
    public bool Success { get; init; }
    public string? FailureReason { get; init; }

    public static MessageDeliveryResult Ok() => new() { Success = true };
    public static MessageDeliveryResult Fail(string reason) => new() { Success = false, FailureReason = reason };
}
