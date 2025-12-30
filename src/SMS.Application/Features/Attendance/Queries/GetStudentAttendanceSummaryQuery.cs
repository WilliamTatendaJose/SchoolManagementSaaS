using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Attendance.Queries;

public record GetStudentAttendanceSummaryQuery : IRequest<Result<AttendanceSummaryDto>>
{
    public Guid StudentId { get; init; }
    public Guid? AcademicTermId { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
}

public record AttendanceSummaryDto
{
    public int TotalDays { get; init; }
    public int PresentDays { get; init; }
    public int AbsentDays { get; init; }
    public int LateDays { get; init; }
    public int ExcusedDays { get; init; }
    public decimal AttendancePercentage { get; init; }
}
