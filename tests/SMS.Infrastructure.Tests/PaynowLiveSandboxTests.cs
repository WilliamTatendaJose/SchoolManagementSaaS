using System.Globalization;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SMS.Application.Interfaces;
using SMS.Infrastructure.Services.Payments;
using Xunit;
using Xunit.Abstractions;

namespace SMS.Infrastructure.Tests;

/// <summary>
/// Exercises PaynowPaymentGatewayService against the real Paynow endpoint. Reads
/// credentials from environment variables rather than source so a real merchant
/// key is never committed. Set PAYNOW_TEST_INTEGRATION_ID and
/// PAYNOW_TEST_INTEGRATION_KEY (from Paynow's merchant dashboard -> Integrations)
/// before running; the test self-skips when they're unset, so it never blocks a
/// normal `dotnet test` run.
/// </summary>
public class PaynowLiveSandboxTests
{
    private static readonly string? IntegrationId = Environment.GetEnvironmentVariable("PAYNOW_TEST_INTEGRATION_ID");
    private static readonly string? IntegrationKey = Environment.GetEnvironmentVariable("PAYNOW_TEST_INTEGRATION_KEY");

    private readonly ITestOutputHelper _output;

    public PaynowLiveSandboxTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static PaynowPaymentGatewayService CreateService() => new(
        new HttpClient(),
        Options.Create(new PaynowOptions
        {
            IntegrationId = IntegrationId ?? string.Empty,
            IntegrationKey = IntegrationKey ?? string.Empty,
            InitiateUrl = "https://www.paynow.co.zw/interface/initiatetransaction",
            ResultUrlBase = "https://example.com/api/payments/paynow/result",
            ReturnUrl = "https://example.com/payments/return"
        }),
        NullLogger<PaynowPaymentGatewayService>.Instance);

    [Fact]
    public async Task Live_initiate_against_paynow_returns_a_redirect_and_poll_url()
    {
        if (IntegrationId is null || IntegrationKey is null)
        {
            _output.WriteLine("Skipped: set PAYNOW_TEST_INTEGRATION_ID / PAYNOW_TEST_INTEGRATION_KEY to run this against a real account.");
            return;
        }

        var service = CreateService();
        var result = await service.InitiatePaymentAsync(new PaymentInitiationRequest
        {
            Reference = $"SANDBOX-TEST-{DateTime.UtcNow:yyyyMMddHHmmss}",
            Amount = 1.00m,
            ItemDescription = "Sandbox connectivity check"
        });

        _output.WriteLine($"Success={result.Success} Error={result.Error} RedirectUrl={result.RedirectUrl} PollUrl={result.PollUrl}");

        result.Success.Should().BeTrue(because: result.Error ?? "expected Paynow to accept the request");
        result.RedirectUrl.Should().NotBeNullOrWhiteSpace();
        result.PollUrl.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Live_poll_of_a_freshly_initiated_transaction_returns_a_verifiable_status()
    {
        if (IntegrationId is null || IntegrationKey is null)
        {
            _output.WriteLine("Skipped: set PAYNOW_TEST_INTEGRATION_ID / PAYNOW_TEST_INTEGRATION_KEY to run this against a real account.");
            return;
        }

        var service = CreateService();
        var initiation = await service.InitiatePaymentAsync(new PaymentInitiationRequest
        {
            Reference = $"SANDBOX-POLL-{DateTime.UtcNow:yyyyMMddHHmmss}",
            Amount = 1.00m,
            ItemDescription = "Sandbox poll check"
        });
        initiation.Success.Should().BeTrue(because: initiation.Error ?? "initiation must succeed before polling");

        var status = await service.CheckStatusAsync(initiation.PollUrl!);
        _output.WriteLine($"IsValid={status.IsValid} Error={status.Error} Status={status.Status}");

        status.IsValid.Should().BeTrue(because: status.Error ?? "poll response hash should verify against our integration key");
    }

    /// <summary>Raw diagnostic: prints exactly what was sent and Paynow's raw response,
    /// for debugging a hash/credential mismatch without the service layer in the way.</summary>
    [Fact]
    public async Task Diagnostic_raw_call_shows_exactly_what_paynow_rejects()
    {
        if (IntegrationId is null || IntegrationKey is null)
        {
            _output.WriteLine("Skipped: set PAYNOW_TEST_INTEGRATION_ID / PAYNOW_TEST_INTEGRATION_KEY to run this against a real account.");
            return;
        }

        var fields = new List<KeyValuePair<string, string>>
        {
            new("id", IntegrationId),
            new("reference", $"SANDBOX-DIAG-{DateTime.UtcNow:yyyyMMddHHmmss}"),
            new("amount", (1.00m).ToString("0.00", CultureInfo.InvariantCulture)),
            new("additionalinfo", "Sandbox connectivity check"),
            new("returnurl", "https://example.com/payments/return"),
            new("resulturl", "https://example.com/api/payments/paynow/result"),
            new("authemail", string.Empty),
            new("status", "Message"),
        };

        var hash = PaynowSignature.Hash(fields.Select(f => f.Value), IntegrationKey);
        _output.WriteLine($"Computed hash: {hash}");
        foreach (var f in fields) _output.WriteLine($"{f.Key} = {f.Value}");
        fields.Add(new("hash", hash));

        using var client = new HttpClient();
        using var content = new FormUrlEncodedContent(fields);
        using var response = await client.PostAsync("https://www.paynow.co.zw/interface/initiatetransaction", content);
        var body = await response.Content.ReadAsStringAsync();

        _output.WriteLine($"HTTP status: {response.StatusCode}");
        _output.WriteLine($"Raw response body: {body}");

        body.Should().NotBeNullOrEmpty();
    }
}
