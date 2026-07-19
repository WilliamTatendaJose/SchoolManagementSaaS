namespace SMS.Application.Interfaces;

/// <summary>
/// A delivery channel for outbound messages (SMS, WhatsApp, ...). Implementations live in
/// the Infrastructure layer and are selected by <see cref="Channel"/> at dispatch time.
/// </summary>
public interface IMessageChannel
{
    /// <summary>Channel identifier, e.g. "SMS" or "WhatsApp".</summary>
    string Channel { get; }

    /// <summary>
    /// True when the provider confirms delivery asynchronously (e.g. WhatsApp status
    /// webhooks). For such channels a successful send is only <em>accepted</em> ("Sent"),
    /// and delivery is confirmed later; for channels without receipts a successful send is
    /// treated as delivered immediately.
    /// </summary>
    bool SupportsDeliveryReceipts => false;

    Task<MessageDeliveryResult> SendAsync(string recipient, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends using a pre-approved, named template (e.g. a WhatsApp Business template),
    /// identified by <paramref name="templateKey"/> - the sending <c>Message</c>'s
    /// <c>MessageType</c> (see <c>MessageTypes</c>), which a channel maps to its own
    /// provider-specific template name/language via its own configuration. Channels that
    /// don't support templates (SMS) or have no template configured for this key fall back
    /// to a plain <see cref="SendAsync"/> of <paramref name="fallbackContent"/> by default.
    ///
    /// This matters for WhatsApp specifically: Meta only allows freeform business-initiated
    /// messages inside a 24h window opened by the recipient; outside it, only approved
    /// templates are delivered. Fee reminders, discipline notices and assignment reminders
    /// are always business-initiated, so they route through here.
    /// </summary>
    Task<MessageDeliveryResult> SendTemplateAsync(
        string recipient, string templateKey, IReadOnlyList<string> parameters, string fallbackContent, CancellationToken cancellationToken = default)
        => SendAsync(recipient, fallbackContent, cancellationToken);
}

public record MessageDeliveryResult
{
    public bool Success { get; init; }
    public string? FailureReason { get; init; }

    /// <summary>
    /// The provider's own id for the accepted message (e.g. a WhatsApp <c>wamid</c>), used
    /// to correlate later delivery-status webhooks back to the recipient. Null when the
    /// provider returns none.
    /// </summary>
    public string? ProviderMessageId { get; init; }

    public static MessageDeliveryResult Ok(string? providerMessageId = null)
        => new() { Success = true, ProviderMessageId = providerMessageId };
    public static MessageDeliveryResult Fail(string reason) => new() { Success = false, FailureReason = reason };
}
