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

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ApiUrl) && !string.IsNullOrWhiteSpace(AccessToken);
}
