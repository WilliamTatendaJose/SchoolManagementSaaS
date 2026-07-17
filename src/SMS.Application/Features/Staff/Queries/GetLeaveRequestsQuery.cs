using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Staff.Queries;

public record GetLeaveRequestsQuery : IRequest<Result<List<LeaveRequestDto>>>
{
    public Guid? StaffId { get; init; }
    public string? Status { get; init; }
}

public record LeaveRequestDto
{
    public Guid Id { get; init; }
    public Guid StaffId { get; init; }
    public string StaffName { get; init; } = string.Empty;
    public string LeaveType { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public int NumberOfDays { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? Reason { get; init; }
    public string? ApproverComments { get; init; }
    public DateTime? ApprovedAt { get; init; }
}
