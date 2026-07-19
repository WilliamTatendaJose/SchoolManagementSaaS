using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SMS.Infrastructure.Services.Messaging;
using Xunit;

namespace SMS.Infrastructure.Tests;

/// <summary>
/// Captures the last outgoing request instead of making a real HTTP call, so the payload
/// WhatsAppChannel builds can be inspected directly.
/// </summary>
internal sealed class CapturingHandler : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }
    public string? LastBody { get; private set; }
    public HttpStatusCode ResponseStatus { get; set; } = HttpStatusCode.OK;
    public string ResponseBody { get; set; } = """{"messages":[{"id":"wamid.CAPTURED"}]}""";

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        LastBody = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        return new HttpResponseMessage(ResponseStatus) { Content = new StringContent(ResponseBody, Encoding.UTF8, "application/json") };
    }
}

/// <summary>
/// Verifies WhatsAppChannel.SendTemplateAsync builds a real "type: template" payload with
/// the configured template name/language/ordered params when one is mapped for the
/// message type, and falls back to plain "type: text" (the freeform path) when none is
/// configured for that key - the behaviour that keeps freeform sends working inside
/// WhatsApp's 24h window while templates cover business-initiated notifications outside it.
/// </summary>
public class WhatsAppTemplateTests
{
    private static WhatsAppChannel CreateChannel(CapturingHandler handler, Dictionary<string, WhatsAppTemplateConfig>? templates = null)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = null };
        return new WhatsAppChannel(
            httpClient,
            Options.Create(new WhatsAppOptions
            {
                ApiUrl = "https://graph.facebook.com/v21.0",
                AccessToken = "test-token",
                PhoneNumberId = "12345",
                Templates = templates ?? []
            }),
            NullLogger<WhatsAppChannel>.Instance);
    }

    [Fact]
    public async Task SendTemplateAsync_with_a_configured_template_sends_a_type_template_payload()
    {
        var handler = new CapturingHandler();
        var channel = CreateChannel(handler, new Dictionary<string, WhatsAppTemplateConfig>
        {
            ["FeeReminder"] = new() { Name = "fee_reminder_v1", LanguageCode = "en_US" }
        });

        var result = await channel.SendTemplateAsync(
            "+263771000001", "FeeReminder", ["Tino Moyo", "USD 150.00"], "fallback text should not be used");

        result.Success.Should().BeTrue();
        result.ProviderMessageId.Should().Be("wamid.CAPTURED");

        handler.LastBody.Should().NotBeNull();
        using var doc = JsonDocument.Parse(handler.LastBody!);
        var root = doc.RootElement;

        root.GetProperty("type").GetString().Should().Be("template");
        root.GetProperty("to").GetString().Should().Be("+263771000001");
        var template = root.GetProperty("template");
        template.GetProperty("name").GetString().Should().Be("fee_reminder_v1");
        template.GetProperty("language").GetProperty("code").GetString().Should().Be("en_US");

        var bodyParams = template.GetProperty("components")[0].GetProperty("parameters");
        bodyParams.GetArrayLength().Should().Be(2);
        bodyParams[0].GetProperty("text").GetString().Should().Be("Tino Moyo");
        bodyParams[1].GetProperty("text").GetString().Should().Be("USD 150.00");
    }

    [Fact]
    public async Task SendTemplateAsync_with_no_configured_template_falls_back_to_freeform_text()
    {
        var handler = new CapturingHandler();
        var channel = CreateChannel(handler); // no templates configured

        var result = await channel.SendTemplateAsync(
            "+263771000001", "FeeReminder", ["Tino Moyo", "USD 150.00"], "Dear Parent, fees are due.");

        result.Success.Should().BeTrue();

        using var doc = JsonDocument.Parse(handler.LastBody!);
        var root = doc.RootElement;
        root.GetProperty("type").GetString().Should().Be("text");
        root.GetProperty("text").GetProperty("body").GetString().Should().Be("Dear Parent, fees are due.");
    }

    [Fact]
    public async Task SendTemplateAsync_reports_failure_when_the_graph_api_rejects_the_template()
    {
        var handler = new CapturingHandler { ResponseStatus = HttpStatusCode.BadRequest, ResponseBody = """{"error":"template not approved"}""" };
        var channel = CreateChannel(handler, new Dictionary<string, WhatsAppTemplateConfig>
        {
            ["FeeReminder"] = new() { Name = "fee_reminder_v1", LanguageCode = "en_US" }
        });

        var result = await channel.SendTemplateAsync("+263771000001", "FeeReminder", ["Tino Moyo", "USD 150.00"], "fallback");

        result.Success.Should().BeFalse();
    }
}
