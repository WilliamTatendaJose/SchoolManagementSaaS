using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SMS.Application.Interfaces;

namespace SMS.Infrastructure.Services.Payments;

/// <summary>
/// Paynow (Zimbabwe) implementation of <see cref="IPaymentGatewayService"/>. Paynow
/// aggregates EcoCash, OneMoney, ZimSwitch and card payments. Network/credential
/// failures are surfaced as unsuccessful results rather than thrown, so an unconfigured
/// environment still builds and runs.
/// </summary>
public class PaynowPaymentGatewayService : IPaymentGatewayService
{
    private readonly HttpClient _httpClient;
    private readonly PaynowOptions _options;
    private readonly ILogger<PaynowPaymentGatewayService> _logger;

    public PaynowPaymentGatewayService(
        HttpClient httpClient,
        IOptions<PaynowOptions> options,
        ILogger<PaynowPaymentGatewayService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<PaymentInitiationResult> InitiatePaymentAsync(PaymentInitiationRequest request, CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
        {
            return new PaymentInitiationResult { Success = false, Error = "Paynow is not configured" };
        }

        // Field order matters: the hash is built over the values in this order.
        var fields = new List<KeyValuePair<string, string>>
        {
            new("id", _options.IntegrationId),
            new("reference", request.Reference),
            new("amount", request.Amount.ToString("0.00", CultureInfo.InvariantCulture)),
            new("additionalinfo", request.ItemDescription),
            new("returnurl", _options.ReturnUrl ?? string.Empty),
            new("resulturl", BuildResultUrl(request.TenantId)),
            new("authemail", request.Email ?? string.Empty),
            new("status", "Message")
        };

        var hash = PaynowSignature.Hash(fields.Select(f => f.Value), _options.IntegrationKey);
        fields.Add(new("hash", hash));

        try
        {
            using var content = new FormUrlEncodedContent(fields);
            using var response = await _httpClient.PostAsync(_options.InitiateUrl, content, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var parsed = ParseUrlEncoded(body);

            var status = GetValue(parsed, "status");
            if (!string.Equals(status, "Ok", StringComparison.OrdinalIgnoreCase))
            {
                return new PaymentInitiationResult { Success = false, Error = GetValue(parsed, "error") ?? "Paynow rejected the request" };
            }

            if (!PaynowSignature.Verify(parsed, _options.IntegrationKey))
            {
                _logger.LogWarning("Paynow initiation response hash verification failed for reference {Reference}", request.Reference);
                return new PaymentInitiationResult { Success = false, Error = "Response verification failed" };
            }

            return new PaymentInitiationResult
            {
                Success = true,
                RedirectUrl = GetValue(parsed, "browserurl"),
                PollUrl = GetValue(parsed, "pollurl"),
                Instructions = GetValue(parsed, "instructions")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Paynow initiation failed for reference {Reference}", request.Reference);
            return new PaymentInitiationResult { Success = false, Error = "Could not reach the payment gateway" };
        }
    }

    public async Task<PaymentStatusResult> CheckStatusAsync(string pollUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.PostAsync(pollUrl, new FormUrlEncodedContent([]), cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseStatusFields(ParseUrlEncoded(body));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Paynow status check failed for {PollUrl}", pollUrl);
            return new PaymentStatusResult { IsValid = false, Error = "Could not reach the payment gateway" };
        }
    }

    public PaymentStatusResult ParseStatusCallback(IReadOnlyList<KeyValuePair<string, string>> fields)
        => ParseStatusFields(fields);

    private PaymentStatusResult ParseStatusFields(IReadOnlyList<KeyValuePair<string, string>> fields)
    {
        if (!PaynowSignature.Verify(fields, _options.IntegrationKey))
        {
            return new PaymentStatusResult { IsValid = false, Error = "Hash verification failed" };
        }

        decimal.TryParse(GetValue(fields, "amount"), NumberStyles.Any, CultureInfo.InvariantCulture, out var amount);

        return new PaymentStatusResult
        {
            IsValid = true,
            Reference = GetValue(fields, "reference"),
            GatewayReference = GetValue(fields, "paynowreference"),
            Amount = amount,
            Status = MapStatus(GetValue(fields, "status"))
        };
    }

    internal static GatewayPaymentStatus MapStatus(string? paynowStatus) => (paynowStatus ?? string.Empty).Trim().ToLowerInvariant() switch
    {
        "paid" => GatewayPaymentStatus.Paid,
        "awaiting delivery" => GatewayPaymentStatus.AwaitingDelivery,
        "delivered" => GatewayPaymentStatus.Delivered,
        "created" => GatewayPaymentStatus.Created,
        "sent" => GatewayPaymentStatus.Sent,
        "cancelled" => GatewayPaymentStatus.Cancelled,
        "refunded" => GatewayPaymentStatus.Refunded,
        _ => GatewayPaymentStatus.Failed
    };

    private string BuildResultUrl(Guid? tenantId)
    {
        var baseUrl = _options.ResultUrlBase;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return string.Empty;
        }

        return tenantId.HasValue
            ? $"{baseUrl.TrimEnd('/')}/{tenantId.Value}"
            : baseUrl;
    }

    private static string? GetValue(IReadOnlyList<KeyValuePair<string, string>> fields, string key)
    {
        foreach (var pair in fields)
        {
            if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                return pair.Value;
            }
        }

        return null;
    }

    private static List<KeyValuePair<string, string>> ParseUrlEncoded(string body)
    {
        var result = new List<KeyValuePair<string, string>>();
        if (string.IsNullOrWhiteSpace(body))
        {
            return result;
        }

        foreach (var pair in body.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = pair.IndexOf('=');
            if (idx < 0)
            {
                result.Add(new KeyValuePair<string, string>(Uri.UnescapeDataString(pair), string.Empty));
                continue;
            }

            var key = Uri.UnescapeDataString(pair[..idx]);
            var value = Uri.UnescapeDataString(pair[(idx + 1)..].Replace('+', ' '));
            result.Add(new KeyValuePair<string, string>(key, value));
        }

        return result;
    }
}
