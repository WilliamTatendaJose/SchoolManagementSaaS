using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SMS.Application.Common.Messaging;
using SMS.Application.Interfaces;

namespace SMS.Infrastructure.Services.Messaging;

/// <summary>
/// WhatsApp Business (Meta Graph API) delivery channel. Sends plain-text or approved
/// template messages; builds and runs without live credentials (reports failure until
/// configured).
/// </summary>
public class WhatsAppChannel : IMessageChannel
{
    private readonly HttpClient _httpClient;
    private readonly WhatsAppOptions _options;
    private readonly ILogger<WhatsAppChannel> _logger;

    public WhatsAppChannel(HttpClient httpClient, IOptions<WhatsAppOptions> options, ILogger<WhatsAppChannel> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public string Channel => MessageChannels.WhatsApp;

    // WhatsApp confirms delivery asynchronously via status webhooks, so a 200 from the
    // send API only means "accepted", not "delivered".
    public bool SupportsDeliveryReceipts => true;

    public Task<MessageDeliveryResult> SendAsync(string recipient, string content, CancellationToken cancellationToken = default) =>
        PostAsync(recipient, new
        {
            messaging_product = "whatsapp",
            to = recipient,
            type = "text",
            text = new { body = content }
        }, cancellationToken);

    /// <summary>
    /// Sends the Meta-approved template mapped to <paramref name="templateKey"/> (a
    /// MessageType, e.g. "FeeReminder") in <see cref="WhatsAppOptions.Templates"/>, with
    /// <paramref name="parameters"/> filling the template's body placeholders in order.
    /// Falls back to a freeform send of <paramref name="fallbackContent"/> when no template
    /// is configured for this key - which only succeeds inside WhatsApp's 24h
    /// customer-service window; outside it, Meta rejects the freeform send and this
    /// surfaces as a normal Failed delivery.
    /// </summary>
    public Task<MessageDeliveryResult> SendTemplateAsync(
        string recipient, string templateKey, IReadOnlyList<string> parameters, string fallbackContent, CancellationToken cancellationToken = default)
    {
        if (!_options.Templates.TryGetValue(templateKey, out var template) || string.IsNullOrWhiteSpace(template.Name))
        {
            return SendAsync(recipient, fallbackContent, cancellationToken);
        }

        return PostAsync(recipient, new
        {
            messaging_product = "whatsapp",
            to = recipient,
            type = "template",
            template = new
            {
                name = template.Name,
                language = new { code = template.LanguageCode },
                components = parameters.Count == 0
                    ? Array.Empty<object>()
                    : [new { type = "body", parameters = parameters.Select(p => new { type = "text", text = p }) }]
            }
        }, cancellationToken);
    }

    private async Task<MessageDeliveryResult> PostAsync(string recipient, object payload, CancellationToken cancellationToken)
    {
        if (!_options.IsConfigured)
        {
            return MessageDeliveryResult.Fail("WhatsApp channel is not configured");
        }

        try
        {
            var endpoint = $"{_options.ApiUrl.TrimEnd('/')}/{_options.PhoneNumberId}/messages";
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = JsonContent.Create(payload) };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await SafeReadBodyAsync(response, cancellationToken);
                _logger.LogWarning("WhatsApp API rejected a message to {Recipient}: {Status} {Body}", recipient, (int)response.StatusCode, body);
                return MessageDeliveryResult.Fail($"WhatsApp API returned {(int)response.StatusCode}");
            }

            var messageId = await ExtractMessageIdAsync(response, cancellationToken);
            return MessageDeliveryResult.Ok(messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send WhatsApp message to {Recipient}", recipient);
            return MessageDeliveryResult.Fail("Could not reach the WhatsApp API");
        }
    }

    /// <summary>Reads the accepted message's <c>wamid</c> from <c>messages[0].id</c>.</summary>
    private static async Task<string?> ExtractMessageIdAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            using var doc = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            if (doc.RootElement.TryGetProperty("messages", out var messages)
                && messages.ValueKind == JsonValueKind.Array
                && messages.GetArrayLength() > 0
                && messages[0].TryGetProperty("id", out var id))
            {
                return id.GetString();
            }
        }
        catch (JsonException)
        {
            // A 2xx without a parseable body still counts as accepted; we just have no id.
        }

        return null;
    }

    private static async Task<string> SafeReadBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch
        {
            return "(unreadable body)";
        }
    }
}
