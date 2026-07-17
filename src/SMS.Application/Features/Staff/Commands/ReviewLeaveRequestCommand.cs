using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Staff.Commands;

/// <summary>
/// Approves or rejects a pending leave request.
/// </summary>
public record ReviewLeaveRequestCommand : IRequest<Result>
{
    public Guid LeaveRequestId { get; init; }
    public bool Approve { get; init; }
    public string? Comments { get; init; }
}
