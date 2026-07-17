using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Dashboard.Queries;

/// <summary>Aggregate analytics for the admin dashboard.</summary>
public record GetDashboardQuery : IRequest<Result<DashboardDto>>;

public record DashboardDto
{
    public int ActiveStudents { get; init; }
    public int BoardingStudents { get; init; }
    public int TotalStaff { get; init; }
    public int TotalClasses { get; init; }

    public decimal TotalBilled { get; init; }
    public decimal TotalCollected { get; init; }
    public decimal TotalOutstanding { get; init; }
    public decimal CollectionRatePercent { get; init; }

    public int AttendanceRatePercent { get; init; }

    public List<ClassEnrollmentDto> EnrollmentByClass { get; init; } = [];
}

public record ClassEnrollmentDto
{
    public string ClassName { get; init; } = string.Empty;
    public int StudentCount { get; init; }
}
