using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SMS.Application.Common.Messaging;
using SMS.Application.Interfaces;

namespace SMS.Infrastructure.Services.Messaging;

/// <summary>
/// WhatsApp Business (Meta Graph API) delivery channel. Sends a plain-text message; builds
/// and runs without live credentials (reports failure until configured).
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

    public async Task<MessageDeliveryResult> SendAsync(string recipient, string content, CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
        {
            return MessageDeliveryResult.Fail("WhatsApp channel is not configured");
        }

        try
        {
            var endpoint = $"{_options.ApiUrl.TrimEnd('/')}/{_options.PhoneNumberId}/messages";
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(new
                {
                    messaging_product = "whatsapp",
                    to = recipient,
                    type = "text",
                    text = new { body = content }
                })
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode
                ? MessageDeliveryResult.Ok()
                : MessageDeliveryResult.Fail($"WhatsApp API returned {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send WhatsApp message to {Recipient}", recipient);
            return MessageDeliveryResult.Fail("Could not reach the WhatsApp API");
        }
    }
}
