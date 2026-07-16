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
}
