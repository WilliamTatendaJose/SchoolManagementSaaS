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

    /// <summary>
    /// Overrides the <c>authemail</c> sent to Paynow. While the integration is in Paynow's
    /// <em>test</em> mode, this must equal the merchant's registered email or Paynow rejects
    /// the request; set it to that address so payments go through regardless of the payer
    /// email the UI collects. In production, leave it empty so the actual payer's email is
    /// used (Paynow emails them a receipt).
    /// </summary>
    public string? AuthEmail { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(IntegrationId) && !string.IsNullOrWhiteSpace(IntegrationKey);
}
