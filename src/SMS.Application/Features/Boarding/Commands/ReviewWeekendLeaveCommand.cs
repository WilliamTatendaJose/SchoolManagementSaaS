using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Boarding.Commands;

/// <summary>
/// Approves or rejects a pending weekend-leave request. On approval the student's guardian
/// is notified by SMS.
/// </summary>
public record ReviewWeekendLeaveCommand : IRequest<Result>
{
    public Guid LeaveId { get; init; }
    public bool Approve { get; init; }
    public string? ReviewNote { get; init; }
    public bool NotifyGuardian { get; init; } = true;
}
