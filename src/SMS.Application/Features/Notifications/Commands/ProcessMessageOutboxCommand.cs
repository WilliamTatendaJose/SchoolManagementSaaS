using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Notifications.Commands;

/// <summary>
/// Dispatches queued messages whose scheduled time has arrived (or that have no schedule).
/// Invoked by the background outbox processor; runs across tenants.
/// </summary>
public record ProcessMessageOutboxCommand : IRequest<Result<int>>
{
    /// <summary>Maximum messages to process in one pass.</summary>
    public int BatchSize { get; init; } = 50;
}
