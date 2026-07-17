using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SMS.Application.Interfaces;
using SMS.Infrastructure.Services.Messaging;

namespace SMS.Infrastructure.Services;

/// <summary>
/// SMS provider integration. Uses a configured HTTP API (Africa's Talking-style) when
/// credentials are present; without configuration it logs and reports failure rather than
/// pretending to send, so an unconfigured environment still builds and runs.
/// </summary>
public class SmsService : ISmsService
{
    private readonly HttpClient _httpClient;
    private readonly SmsOptions _options;
    private readonly ILogger<SmsService> _logger;

    public SmsService(HttpClient httpClient, IOptions<SmsOptions> options, ILogger<SmsService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
        {
            _logger.LogWarning("SMS provider is not configured; message to {PhoneNumber} was not sent", phoneNumber);
            return false;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, _options.ApiUrl)
            {
                Content = JsonContent.Create(new
                {
                    to = phoneNumber,
                    from = _options.SenderId,
                    message
                })
            };
            request.Headers.TryAddWithoutValidation("ApiKey", _options.ApiKey);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("SMS provider returned {StatusCode} for {PhoneNumber}", response.StatusCode, phoneNumber);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS to {PhoneNumber}", phoneNumber);
            return false;
        }
    }

    public async Task<int> SendBulkSmsAsync(IEnumerable<(string PhoneNumber, string Message)> messages, CancellationToken cancellationToken = default)
    {
        var successCount = 0;

        foreach (var (phoneNumber, message) in messages)
        {
            if (await SendSmsAsync(phoneNumber, message, cancellationToken))
            {
                successCount++;
            }
        }

        return successCount;
    }

    public Task<int> GetRemainingCreditsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        // Real credit balances would be queried from the provider or a per-tenant ledger.
        return Task.FromResult(0);
    }
}
