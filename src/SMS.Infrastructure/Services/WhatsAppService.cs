using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SMS.Application.Interfaces;

namespace SMS.Infrastructure.Services;

/// <summary>
/// WhatsApp service implementation backed by the Meta WhatsApp Business Cloud API.
/// When credentials are not configured the service logs and simulates delivery so
/// that development and testing can proceed without a live provider.
/// </summary>
public class WhatsAppService : IWhatsAppService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WhatsAppService> _logger;

    private readonly string? _accessToken;
    private readonly string? _phoneNumberId;
    private readonly string _apiVersion;
    private readonly string _defaultLanguageCode;

    public WhatsAppService(HttpClient httpClient, IConfiguration configuration, ILogger<WhatsAppService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _accessToken = configuration["WhatsApp:AccessToken"];
        _phoneNumberId = configuration["WhatsApp:PhoneNumberId"];
        _apiVersion = string.IsNullOrWhiteSpace(configuration["WhatsApp:ApiVersion"]) ? "v21.0" : configuration["WhatsApp:ApiVersion"]!;
        _defaultLanguageCode = string.IsNullOrWhiteSpace(configuration["WhatsApp:DefaultLanguageCode"]) ? "en" : configuration["WhatsApp:DefaultLanguageCode"]!;

        if (_httpClient.BaseAddress is null)
        {
            var baseUrl = string.IsNullOrWhiteSpace(configuration["WhatsApp:ApiBaseUrl"])
                ? "https://graph.facebook.com"
                : configuration["WhatsApp:ApiBaseUrl"]!;
            _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        }
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_accessToken) && !string.IsNullOrWhiteSpace(_phoneNumberId);

    public Task<bool> SendMessageAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            messaging_product = "whatsapp",
            recipient_type = "individual",
            to = NormalizePhoneNumber(phoneNumber),
            type = "text",
            text = new { preview_url = false, body = message }
        };

        return SendAsync(phoneNumber, payload, cancellationToken);
    }

    public Task<bool> SendTemplateAsync(string phoneNumber, string templateName, string languageCode, IEnumerable<string> parameters, CancellationToken cancellationToken = default)
    {
        var components = new List<object>();
        var componentParameters = parameters
            .Select(p => new { type = "text", text = p })
            .ToList();

        if (componentParameters.Count > 0)
        {
            components.Add(new { type = "body", parameters = componentParameters });
        }

        var payload = new
        {
            messaging_product = "whatsapp",
            recipient_type = "individual",
            to = NormalizePhoneNumber(phoneNumber),
            type = "template",
            template = new
            {
                name = templateName,
                language = new { code = string.IsNullOrWhiteSpace(languageCode) ? _defaultLanguageCode : languageCode },
                components
            }
        };

        return SendAsync(phoneNumber, payload, cancellationToken);
    }

    public async Task<int> SendBulkMessageAsync(IEnumerable<(string PhoneNumber, string Message)> messages, CancellationToken cancellationToken = default)
    {
        var successCount = 0;

        foreach (var (phoneNumber, message) in messages)
        {
            if (await SendMessageAsync(phoneNumber, message, cancellationToken))
            {
                successCount++;
            }
        }

        return successCount;
    }

    private async Task<bool> SendAsync<TPayload>(string phoneNumber, TPayload payload, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            // Placeholder mode: no credentials configured, so simulate delivery.
            _logger.LogInformation(
                "WhatsApp not configured; simulating message to {PhoneNumber}", phoneNumber);
            return true;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_apiVersion}/{_phoneNumberId}/messages")
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("WhatsApp message sent to {PhoneNumber}", phoneNumber);
                return true;
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Failed to send WhatsApp message to {PhoneNumber}. Status: {StatusCode}. Response: {Response}",
                phoneNumber, response.StatusCode, body);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending WhatsApp message to {PhoneNumber}", phoneNumber);
            return false;
        }
    }

    /// <summary>
    /// The Cloud API expects numbers in E.164 form without a leading '+' or any separators.
    /// </summary>
    private static string NormalizePhoneNumber(string phoneNumber)
    {
        return Regex.Replace(phoneNumber ?? string.Empty, "[^0-9]", string.Empty);
    }
}
