using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Timetable.Commands;

/// <summary>
/// Adds a lesson to the timetable, rejecting class/teacher/classroom clashes.
/// </summary>
public record CreateTimetableSlotCommand : IRequest<Result<Guid>>
{
    public Guid ClassId { get; init; }
    public Guid SubjectId { get; init; }
    public Guid TeacherId { get; init; }
    public Guid? ClassroomId { get; init; }
    public Guid AcademicTermId { get; init; }
    public DayOfWeek DayOfWeek { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
}
