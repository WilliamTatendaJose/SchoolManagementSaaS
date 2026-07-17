using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Staff.Commands;

/// <summary>
/// Submits a leave request for a staff member (status starts as Pending).
/// </summary>
public record CreateLeaveRequestCommand : IRequest<Result<Guid>>
{
    public Guid StaffId { get; init; }
    public string LeaveType { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string? Reason { get; init; }
}
