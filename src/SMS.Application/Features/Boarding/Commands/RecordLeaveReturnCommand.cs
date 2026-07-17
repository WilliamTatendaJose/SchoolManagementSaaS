using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Boarding.Commands;

/// <summary>Signs a departed boarder back in on return. Transitions Departed -&gt; Returned.</summary>
public record RecordLeaveReturnCommand : IRequest<Result>
{
    public Guid LeaveId { get; init; }
    public DateTime? ReturnedAt { get; init; }
}
