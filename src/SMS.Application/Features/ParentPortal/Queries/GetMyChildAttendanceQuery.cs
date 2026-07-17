using MediatR;
using SMS.Application.Common.Models;
using SMS.Application.Features.Attendance.Queries;

namespace SMS.Application.Features.ParentPortal.Queries;

/// <summary>Attendance summary for one of the signed-in parent's children.</summary>
public record GetMyChildAttendanceQuery : IRequest<Result<AttendanceSummaryDto>>
{
    public Guid StudentId { get; init; }
    public Guid? AcademicTermId { get; init; }
}
