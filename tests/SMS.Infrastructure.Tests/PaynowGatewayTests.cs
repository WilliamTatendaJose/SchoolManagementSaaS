using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SMS.Application.Interfaces;
using SMS.Infrastructure.Services.Payments;
using Xunit;

namespace SMS.Infrastructure.Tests;

public class PaynowGatewayTests
{
    private const string IntegrationKey = "test-integration-key";

    private static PaynowPaymentGatewayService CreateService() => new(
        new HttpClient(),
        Options.Create(new PaynowOptions { IntegrationId = "1234", IntegrationKey = IntegrationKey }),
        NullLogger<PaynowPaymentGatewayService>.Instance);

    private static List<KeyValuePair<string, string>> SignedCallback(string status, string amount)
    {
        var fields = new List<KeyValuePair<string, string>>
        {
            new("reference", "PAY-INV-0001"),
            new("paynowreference", "9876543"),
            new("amount", amount),
            new("status", status),
            new("pollurl", "https://www.paynow.co.zw/interface/pollstatus")
        };

        var hash = PaynowSignature.Hash(fields.Select(f => f.Value), IntegrationKey);
        fields.Add(new KeyValuePair<string, string>("hash", hash));
        return fields;
    }

    [Fact]
    public void Hash_is_deterministic_uppercase_sha512_hex()
    {
        var hash = PaynowSignature.Hash(["a", "b", "c"], IntegrationKey);

        hash.Should().HaveLength(128);
        hash.Should().MatchRegex("^[0-9A-F]+$");
        hash.Should().Be(PaynowSignature.Hash(["a", "b", "c"], IntegrationKey));
    }

    [Fact]
    public void ParseStatusCallback_verifies_a_correctly_signed_paid_callback()
    {
        var result = CreateService().ParseStatusCallback(SignedCallback("Paid", "600.00"));

        result.IsValid.Should().BeTrue();
        result.Status.Should().Be(GatewayPaymentStatus.Paid);
        result.IsPaid.Should().BeTrue();
        result.Amount.Should().Be(600.00m);
        result.Reference.Should().Be("PAY-INV-0001");
        result.GatewayReference.Should().Be("9876543");
    }

    [Fact]
    public void ParseStatusCallback_rejects_a_tampered_callback()
    {
        var fields = SignedCallback("Paid", "600.00");
        // Tamper the amount after the hash was computed.
        var amountIndex = fields.FindIndex(f => f.Key == "amount");
        fields[amountIndex] = new KeyValuePair<string, string>("amount", "6000.00");

        var result = CreateService().ParseStatusCallback(fields);

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("Paid", GatewayPaymentStatus.Paid)]
    [InlineData("Awaiting Delivery", GatewayPaymentStatus.AwaitingDelivery)]
    [InlineData("Delivered", GatewayPaymentStatus.Delivered)]
    [InlineData("Cancelled", GatewayPaymentStatus.Cancelled)]
    [InlineData("Refunded", GatewayPaymentStatus.Refunded)]
    [InlineData("Sent", GatewayPaymentStatus.Sent)]
    [InlineData("Something Unknown", GatewayPaymentStatus.Failed)]
    public void MapStatus_maps_paynow_vocabulary(string paynowStatus, GatewayPaymentStatus expected)
    {
        PaynowPaymentGatewayService.MapStatus(paynowStatus).Should().Be(expected);
    }

    private static PaynowPaymentGatewayService ServiceWith(CapturingHandler handler, string? authEmail)
        => new(
            new HttpClient(handler),
            Options.Create(new PaynowOptions
            {
                IntegrationId = "1234",
                IntegrationKey = IntegrationKey,
                InitiateUrl = "https://paynow.example/initiate",
                AuthEmail = authEmail
            }),
            NullLogger<PaynowPaymentGatewayService>.Instance);

    private static Dictionary<string, string> ParseForm(string body) =>
        body.Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split('=', 2))
            .ToDictionary(
                p => Uri.UnescapeDataString(p[0]),
                p => p.Length > 1 ? Uri.UnescapeDataString(p[1].Replace('+', ' ')) : string.Empty);

    [Fact]
    public async Task Initiate_sends_the_configured_AuthEmail_over_the_payer_email()
    {
        // Paynow test mode requires authemail == merchant email; the configured AuthEmail
        // must win over whatever the UI passed as the payer's email.
        var handler = new CapturingHandler { ResponseBody = "status=Error&error=stub" };
        var service = ServiceWith(handler, authEmail: "merchant@example.com");

        await service.InitiatePaymentAsync(new PaymentInitiationRequest
        {
            Reference = "R1", Amount = 5m, Email = "payer@example.com"
        });

        ParseForm(handler.LastBody!)["authemail"].Should().Be("merchant@example.com");
    }

    [Fact]
    public async Task Initiate_falls_back_to_the_payer_email_when_AuthEmail_is_empty()
    {
        var handler = new CapturingHandler { ResponseBody = "status=Error&error=stub" };
        var service = ServiceWith(handler, authEmail: null);

        await service.InitiatePaymentAsync(new PaymentInitiationRequest
        {
            Reference = "R1", Amount = 5m, Email = "payer@example.com"
        });

        ParseForm(handler.LastBody!)["authemail"].Should().Be("payer@example.com");
    }
}
