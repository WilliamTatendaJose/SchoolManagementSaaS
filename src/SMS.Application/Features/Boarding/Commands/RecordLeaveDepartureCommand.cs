using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Boarding.Commands;

/// <summary>Signs an approved boarder out at the gate. Transitions Approved -&gt; Departed.</summary>
public record RecordLeaveDepartureCommand : IRequest<Result>
{
    public Guid LeaveId { get; init; }
    public string CollectedBy { get; init; } = string.Empty;
    public DateTime? DepartedAt { get; init; }
}
