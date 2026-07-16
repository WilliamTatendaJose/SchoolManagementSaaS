namespace SMS.Infrastructure.Services.Payments;

/// <summary>
/// Configuration for the Paynow gateway. Values are supplied via the "Paynow" config
/// section; placeholders let the app build and run without live credentials (real
/// initiation calls simply fail until credentials are provided).
/// </summary>
public class PaynowOptions
{
    public const string SectionName = "Paynow";

    public string IntegrationId { get; set; } = string.Empty;
    public string IntegrationKey { get; set; } = string.Empty;
    public string InitiateUrl { get; set; } = "https://www.paynow.co.zw/interface/initiatetransaction";

    /// <summary>
    /// Base URL Paynow posts the result to. The tenant id is appended so the
    /// unauthenticated callback can be scoped to the right tenant.
    /// </summary>
    public string? ResultUrlBase { get; set; }

    /// <summary>URL the payer's browser is returned to after paying.</summary>
    public string? ReturnUrl { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(IntegrationId) && !string.IsNullOrWhiteSpace(IntegrationKey);
}
