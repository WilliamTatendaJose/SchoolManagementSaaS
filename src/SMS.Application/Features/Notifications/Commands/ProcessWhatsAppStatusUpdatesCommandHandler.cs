using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Messaging;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Notifications.Commands;

public class ProcessWhatsAppStatusUpdatesCommandHandler : IRequestHandler<ProcessWhatsAppStatusUpdatesCommand, Result<int>>
{
    private readonly IApplicationDbContext _context;

    public ProcessWhatsAppStatusUpdatesCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<int>> Handle(ProcessWhatsAppStatusUpdatesCommand request, CancellationToken cancellationToken)
    {
        var wamids = request.Updates
            .Select(u => u.ProviderMessageId)
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .ToList();

        if (wamids.Count == 0)
        {
            return Result<int>.Success(0);
        }

        // Unauthenticated webhook: no tenant context, so match across all tenants by the
        // globally-unique wamid. Loading via the parent messages lets us roll up counts
        // over each message's full recipient set (EF identity resolution ensures the same
        // tracked recipient instances are reused).
        var messages = await _context.Messages
            .IgnoreQueryFilters()
            .Include(m => m.Recipients)
            .Where(m => m.Recipients.Any(r => r.ProviderMessageId != null && wamids.Contains(r.ProviderMessageId)))
            .ToListAsync(cancellationToken);

        var recipientByWamid = messages
            .SelectMany(m => m.Recipients)
            .Where(r => r.ProviderMessageId != null && wamids.Contains(r.ProviderMessageId))
            .GroupBy(r => r.ProviderMessageId!)
            .ToDictionary(g => g.Key, g => g.First());

        var updated = 0;
        foreach (var update in request.Updates)
        {
            if (!recipientByWamid.TryGetValue(update.ProviderMessageId, out var recipient))
            {
                continue;
            }

            var newStatus = MapStatus(update.Status);
            if (newStatus == null)
            {
                continue;
            }

            // Advance only: out-of-order events (e.g. a late "delivered" after "read", or a
            // stray "failed" after delivery) must never regress a recipient's status.
            if (RecipientStatuses.RankOf(newStatus) <= RecipientStatuses.RankOf(recipient.Status))
            {
                continue;
            }

            var timestamp = update.Timestamp ?? DateTime.UtcNow;
            recipient.Status = newStatus;

            switch (newStatus)
            {
                case RecipientStatuses.Delivered:
                    recipient.DeliveredAt = timestamp;
                    break;
                case RecipientStatuses.Read:
                    recipient.ReadAt = timestamp;
                    recipient.DeliveredAt ??= timestamp; // read implies delivered
                    break;
                case RecipientStatuses.Failed:
                    recipient.FailureReason = update.ErrorTitle ?? "Delivery failed";
                    break;
            }

            updated++;
        }

        if (updated > 0)
        {
            foreach (var message in messages)
            {
                MessageDispatcher.RollUpCounts(message);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        return Result<int>.Success(updated);
    }

    private static string? MapStatus(string metaStatus) => metaStatus.Trim().ToLowerInvariant() switch
    {
        "sent" => RecipientStatuses.Sent,
        "delivered" => RecipientStatuses.Delivered,
        "read" => RecipientStatuses.Read,
        "failed" => RecipientStatuses.Failed,
        _ => null
    };
}
