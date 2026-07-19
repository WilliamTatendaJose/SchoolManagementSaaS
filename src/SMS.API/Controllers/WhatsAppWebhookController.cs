using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using SMS.Application.Features.Notifications.Commands;
using SMS.Infrastructure.Services.Messaging;

namespace SMS.API.Controllers;

/// <summary>
/// Receives WhatsApp (Meta Graph API) webhooks: the one-time subscription verification
/// handshake (GET) and asynchronous message delivery-status events (POST). Unauthenticated
/// - Meta cannot present a JWT - and rate-limiting is disabled so status bursts are not
/// throttled. Authenticity is established by the verify token (GET) and the
/// X-Hub-Signature-256 HMAC (POST).
/// </summary>
[ApiController]
[Route("api/webhooks/whatsapp")]
[AllowAnonymous]
[DisableRateLimiting]
public class WhatsAppWebhookController : ControllerBase
{
    private readonly WhatsAppOptions _options;
    private readonly ILogger<WhatsAppWebhookController> _logger;

    public WhatsAppWebhookController(IOptions<WhatsAppOptions> options, ILogger<WhatsAppWebhookController> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    private ISender Mediator => HttpContext.RequestServices.GetRequiredService<ISender>();

    /// <summary>
    /// Meta's subscription verification handshake: echo hub.challenge back when the mode is
    /// "subscribe" and the verify token matches the configured one.
    /// </summary>
    [HttpGet]
    public IActionResult Verify(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? verifyToken,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        if (mode == "subscribe"
            && !string.IsNullOrEmpty(_options.VerifyToken)
            && verifyToken == _options.VerifyToken)
        {
            return Content(challenge ?? string.Empty, "text/plain");
        }

        return Forbid();
    }

    /// <summary>Delivery-status events for previously-sent messages.</summary>
    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        Request.EnableBuffering();
        string body;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true))
        {
            body = await reader.ReadToEndAsync(cancellationToken);
        }
        Request.Body.Position = 0;

        if (!SignatureIsValid(body))
        {
            _logger.LogWarning("Rejected a WhatsApp webhook with an invalid or missing signature");
            // 200 so Meta does not retry a payload we will never accept.
            return Ok();
        }

        var updates = ParseStatusUpdates(body);
        if (updates.Count > 0)
        {
            await Mediator.Send(new ProcessWhatsAppStatusUpdatesCommand(updates), cancellationToken);
        }

        return Ok();
    }

    private bool SignatureIsValid(string body)
    {
        // No app secret configured => skip verification (development only).
        if (string.IsNullOrEmpty(_options.AppSecret))
        {
            return true;
        }

        var header = Request.Headers["X-Hub-Signature-256"].ToString();
        const string prefix = "sha256=";
        if (string.IsNullOrEmpty(header) || !header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var provided = header[prefix.Length..];
        var expected = Convert.ToHexString(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(_options.AppSecret), Encoding.UTF8.GetBytes(body)));

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(provided.ToUpperInvariant()));
    }

    /// <summary>
    /// Pulls the delivery-status events out of Meta's nested webhook envelope:
    /// entry[].changes[].value.statuses[].
    /// </summary>
    private List<WhatsAppStatusUpdate> ParseStatusUpdates(string body)
    {
        var updates = new List<WhatsAppStatusUpdate>();

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("entry", out var entries) || entries.ValueKind != JsonValueKind.Array)
            {
                return updates;
            }

            foreach (var entry in entries.EnumerateArray())
            {
                if (!entry.TryGetProperty("changes", out var changes) || changes.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var change in changes.EnumerateArray())
                {
                    if (!change.TryGetProperty("value", out var value)
                        || !value.TryGetProperty("statuses", out var statuses)
                        || statuses.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    foreach (var status in statuses.EnumerateArray())
                    {
                        var id = status.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
                        var statusText = status.TryGetProperty("status", out var stEl) ? stEl.GetString() : null;
                        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(statusText))
                        {
                            continue;
                        }

                        updates.Add(new WhatsAppStatusUpdate
                        {
                            ProviderMessageId = id,
                            Status = statusText,
                            Timestamp = ParseTimestamp(status),
                            ErrorTitle = ParseErrorTitle(status)
                        });
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Could not parse a WhatsApp webhook payload");
        }

        return updates;
    }

    private static DateTime? ParseTimestamp(JsonElement status)
    {
        if (status.TryGetProperty("timestamp", out var ts)
            && ts.ValueKind is JsonValueKind.String or JsonValueKind.Number)
        {
            var raw = ts.ValueKind == JsonValueKind.String ? ts.GetString() : ts.GetRawText();
            if (long.TryParse(raw, out var seconds))
            {
                return DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
            }
        }

        return null;
    }

    private static string? ParseErrorTitle(JsonElement status)
    {
        if (status.TryGetProperty("errors", out var errors)
            && errors.ValueKind == JsonValueKind.Array
            && errors.GetArrayLength() > 0
            && errors[0].TryGetProperty("title", out var title))
        {
            return title.GetString();
        }

        return null;
    }
}
