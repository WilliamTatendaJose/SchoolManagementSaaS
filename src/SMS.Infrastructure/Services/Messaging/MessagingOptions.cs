namespace SMS.Infrastructure.Services.Messaging;

/// <summary>
/// SMS provider configuration. Placeholders let the app build and run without live
/// credentials; sends simply fail (and are recorded as failed) until configured.
/// Shaped for an Africa's Talking-style HTTP API but provider-neutral.
/// </summary>
public class SmsOptions
{
    public const string SectionName = "Sms";

    public string ApiUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ApiUrl) && !string.IsNullOrWhiteSpace(ApiKey);
}

/// <summary>
/// WhatsApp Business (Meta Graph API) configuration. Placeholders keep the build green
/// without live credentials.
/// </summary>
public class WhatsAppOptions
{
    public const string SectionName = "WhatsApp";

    public string ApiUrl { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string PhoneNumberId { get; set; } = string.Empty;

    /// <summary>
    /// The token echoed back to Meta during webhook subscription verification. Must match
    /// the "Verify token" entered in the Meta App Dashboard's webhook configuration.
    /// </summary>
    public string VerifyToken { get; set; } = string.Empty;

    /// <summary>
    /// The Meta app secret, used to verify the <c>X-Hub-Signature-256</c> HMAC on incoming
    /// webhook payloads. When empty, signature verification is skipped (dev only).
    /// </summary>
    public string AppSecret { get; set; } = string.Empty;

    /// <summary>
    /// Maps a message type (see <c>MessageTypes</c> - e.g. "FeeReminder") to the Meta
    /// template that sends it. Templates must already be created and approved in the Meta
    /// Business dashboard; this only records which one to use and in what language. A
    /// message type with no entry here falls back to freeform text (works only inside
    /// WhatsApp's 24h customer-service window).
    /// </summary>
    public Dictionary<string, WhatsAppTemplateConfig> Templates { get; set; } = [];

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ApiUrl) && !string.IsNullOrWhiteSpace(AccessToken);
}

public class WhatsAppTemplateConfig
{
    /// <summary>The template's name exactly as approved in the Meta Business dashboard.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>BCP-47 language code the template was approved under, e.g. "en_US" or "en".</summary>
    public string LanguageCode { get; set; } = "en";
}
