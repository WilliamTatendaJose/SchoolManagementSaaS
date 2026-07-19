using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Notifications.Commands;

/// <summary>
/// Applies a batch of WhatsApp delivery-status events (from the Meta webhook) to the
/// matching <c>MessageRecipient</c> rows, correlated by the provider message id. The
/// webhook is unauthenticated and not tenant-scoped, so recipients are matched across
/// all tenants by their globally-unique <c>wamid</c>.
/// </summary>
public record ProcessWhatsAppStatusUpdatesCommand(IReadOnlyList<WhatsAppStatusUpdate> Updates)
    : IRequest<Result<int>>;

public record WhatsAppStatusUpdate
{
    /// <summary>The WhatsApp <c>wamid</c> this event refers to.</summary>
    public required string ProviderMessageId { get; init; }
    /// <summary>Meta's status string: sent, delivered, read or failed.</summary>
    public required string Status { get; init; }
    public DateTime? Timestamp { get; init; }
    public string? ErrorTitle { get; init; }
}
