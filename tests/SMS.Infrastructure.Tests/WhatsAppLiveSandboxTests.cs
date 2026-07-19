using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SMS.Infrastructure.Services.Messaging;
using Xunit;
using Xunit.Abstractions;

namespace SMS.Infrastructure.Tests;

/// <summary>
/// Exercises the WhatsApp Business (Meta Graph API) channel against the real Graph API.
/// Reads credentials from environment variables rather than source so a live access
/// token is never committed. Set WHATSAPP_TEST_ACCESS_TOKEN and
/// WHATSAPP_TEST_PHONE_NUMBER_ID before running; tests self-skip when unset, so this
/// never blocks a normal `dotnet test` run.
///
/// Only the read-only diagnostic actually runs unprompted - it fetches the phone
/// number's own metadata, which sends nothing to any third party. A live send to a
/// real recipient is a genuine side effect (a real person receives a WhatsApp message)
/// and additionally set WHATSAPP_TEST_RECIPIENT to a real, consenting number.
/// </summary>
public class WhatsAppLiveSandboxTests
{
    private static readonly string? AccessToken = Environment.GetEnvironmentVariable("WHATSAPP_TEST_ACCESS_TOKEN");
    private static readonly string? PhoneNumberId = Environment.GetEnvironmentVariable("WHATSAPP_TEST_PHONE_NUMBER_ID");
    private static readonly string? Recipient = Environment.GetEnvironmentVariable("WHATSAPP_TEST_RECIPIENT");

    private readonly ITestOutputHelper _output;

    public WhatsAppLiveSandboxTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static WhatsAppChannel CreateChannel() => new(
        new HttpClient(),
        Options.Create(new WhatsAppOptions
        {
            ApiUrl = "https://graph.facebook.com/v21.0",
            AccessToken = AccessToken ?? string.Empty,
            PhoneNumberId = PhoneNumberId ?? string.Empty
        }),
        NullLogger<WhatsAppChannel>.Instance);

    /// <summary>Read-only: confirms the access token and phone number ID are valid and
    /// checks the number's messaging quality/verification status. Sends nothing to
    /// anyone, so it's safe to run unprompted whenever credentials are set.</summary>
    [Fact]
    public async Task Diagnostic_phone_number_lookup_confirms_the_token_and_phone_id_are_valid()
    {
        if (AccessToken is null || PhoneNumberId is null)
        {
            _output.WriteLine("Skipped: set WHATSAPP_TEST_ACCESS_TOKEN / WHATSAPP_TEST_PHONE_NUMBER_ID to run this.");
            return;
        }

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);

        var url = $"https://graph.facebook.com/v21.0/{PhoneNumberId}?fields=display_phone_number,verified_name,quality_rating,code_verification_status";
        using var response = await client.GetAsync(url);
        var body = await response.Content.ReadAsStringAsync();

        _output.WriteLine($"HTTP status: {response.StatusCode}");
        _output.WriteLine($"Raw response body: {body}");

        response.IsSuccessStatusCode.Should().BeTrue(because: $"Graph API should accept this token/phone-number-id pair; got: {body}");
    }

    /// <summary>Live send to a real number - genuine side effect. Requires an explicit
    /// recipient env var on top of the credentials so it never fires without a
    /// deliberately supplied, consenting number. Freeform text only works within an
    /// open 24h customer-service window (the recipient must have messaged the
    /// business number recently) - otherwise Meta requires a pre-approved template.</summary>
    [Fact]
    public async Task Live_send_delivers_a_freeform_text_message_to_the_configured_recipient()
    {
        if (AccessToken is null || PhoneNumberId is null || Recipient is null)
        {
            _output.WriteLine("Skipped: set WHATSAPP_TEST_ACCESS_TOKEN / WHATSAPP_TEST_PHONE_NUMBER_ID / WHATSAPP_TEST_RECIPIENT to run this.");
            return;
        }

        var channel = CreateChannel();
        var result = await channel.SendAsync(Recipient, "SMS test: this is a connectivity check from the School Management SaaS WhatsApp integration.");

        _output.WriteLine($"Success={result.Success} FailureReason={result.FailureReason}");

        result.Success.Should().BeTrue(because: result.FailureReason ?? "expected the Graph API to accept the message");
    }

    /// <summary>Same live send, but prints the raw Graph API response body instead of just
    /// the HTTP status - "202/200 accepted" is not the same as "delivered", and the body
    /// carries the real error (e.g. recipient not on the test allow-list) even on a 200.</summary>
    [Fact]
    public async Task Diagnostic_raw_send_shows_the_full_graph_api_response()
    {
        if (AccessToken is null || PhoneNumberId is null || Recipient is null)
        {
            _output.WriteLine("Skipped: set WHATSAPP_TEST_ACCESS_TOKEN / WHATSAPP_TEST_PHONE_NUMBER_ID / WHATSAPP_TEST_RECIPIENT to run this.");
            return;
        }

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);

        var endpoint = $"https://graph.facebook.com/v21.0/{PhoneNumberId}/messages";
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = System.Net.Http.Json.JsonContent.Create(new
            {
                messaging_product = "whatsapp",
                to = Recipient,
                type = "text",
                text = new { body = "Diagnostic: raw Graph API response check." }
            })
        };

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        _output.WriteLine($"HTTP status: {response.StatusCode}");
        _output.WriteLine($"Raw response body: {body}");

        body.Should().NotBeNullOrEmpty();
    }
}
