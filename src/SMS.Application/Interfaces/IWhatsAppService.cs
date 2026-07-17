namespace SMS.Application.Interfaces;

/// <summary>
/// Interface for sending WhatsApp messages via the WhatsApp Business Cloud API
/// </summary>
public interface IWhatsAppService
{
    /// <summary>
    /// Indicates whether the WhatsApp Business API has been configured with credentials.
    /// When false, the service operates in a no-op/simulation mode.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Sends a free-form text message. Note: outside the 24-hour customer service
    /// window the WhatsApp Business API only allows pre-approved template messages.
    /// </summary>
    Task<bool> SendMessageAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a pre-approved template message, which is required for business-initiated
    /// notifications (e.g. attendance, fees, announcements).
    /// </summary>
    Task<bool> SendTemplateAsync(string phoneNumber, string templateName, string languageCode, IEnumerable<string> parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends the same message to multiple recipients and returns the number delivered successfully.
    /// </summary>
    Task<int> SendBulkMessageAsync(IEnumerable<(string PhoneNumber, string Message)> messages, CancellationToken cancellationToken = default);
}
