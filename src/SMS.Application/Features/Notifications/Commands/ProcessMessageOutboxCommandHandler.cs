using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Messaging;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Notifications.Commands;

public class ProcessMessageOutboxCommandHandler : IRequestHandler<ProcessMessageOutboxCommand, Result<int>>
{
    private readonly IApplicationDbContext _context;
    private readonly IEnumerable<IMessageChannel> _channels;

    public ProcessMessageOutboxCommandHandler(IApplicationDbContext context, IEnumerable<IMessageChannel> channels)
    {
        _context = context;
        _channels = channels;
    }

    public async Task<Result<int>> Handle(ProcessMessageOutboxCommand request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        // Cross-tenant: the processor runs without a tenant context, so bypass the filter.
        var due = await _context.Messages
            .IgnoreQueryFilters()
            .Include(m => m.Recipients)
            .Where(m => m.Status == MessageStatuses.Queued
                && (m.ScheduledAt == null || m.ScheduledAt <= now))
            .OrderBy(m => m.ScheduledAt)
            .Take(request.BatchSize)
            .ToListAsync(cancellationToken);

        var processed = 0;

        foreach (var message in due)
        {
            var channel = _channels.FirstOrDefault(c => c.Channel == message.Channel);
            if (channel == null)
            {
                // No channel for this message; mark failed so it isn't retried forever.
                message.Status = MessageStatuses.Failed;
                continue;
            }

            await MessageDispatcher.DispatchPendingAsync(channel, message, cancellationToken);
            processed++;
        }

        if (due.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return Result<int>.Success(processed);
    }
}
