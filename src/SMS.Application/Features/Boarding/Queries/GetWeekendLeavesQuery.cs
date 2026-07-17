using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Boarding.Queries;

/// <summary>
/// Lists weekend-leave records, newest departure first, optionally filtered by student,
/// status, or a departure-date window.
/// </summary>
public record GetWeekendLeavesQuery : IRequest<Result<PaginatedList<WeekendLeaveDto>>>
{
    public Guid? StudentId { get; init; }
    public string? Status { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public record WeekendLeaveDto
{
    public Guid Id { get; init; }
    public Guid StudentId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public string? DormitoryName { get; init; }
    public DateTime DepartureDate { get; init; }
    public DateTime ExpectedReturnDate { get; init; }
    public string Destination { get; init; } = string.Empty;
    public string? Reason { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? ReviewNote { get; init; }
    public string? CollectedBy { get; init; }
    public DateTime? ActualDepartureAt { get; init; }
    public DateTime? ActualReturnAt { get; init; }
    public bool GuardianNotified { get; init; }
}
