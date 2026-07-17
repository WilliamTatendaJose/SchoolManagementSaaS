using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Timetable.Queries;

/// <summary>
/// Returns timetable slots for a term, optionally filtered by class, teacher, or classroom.
/// </summary>
public record GetTimetableQuery : IRequest<Result<List<TimetableSlotDto>>>
{
    public Guid AcademicTermId { get; init; }
    public Guid? ClassId { get; init; }
    public Guid? TeacherId { get; init; }
    public Guid? ClassroomId { get; init; }
}

public record TimetableSlotDto
{
    public Guid Id { get; init; }
    public Guid ClassId { get; init; }
    public string ClassName { get; init; } = string.Empty;
    public Guid SubjectId { get; init; }
    public string SubjectName { get; init; } = string.Empty;
    public Guid TeacherId { get; init; }
    public string TeacherName { get; init; } = string.Empty;
    public Guid? ClassroomId { get; init; }
    public string? ClassroomName { get; init; }
    public string DayOfWeek { get; init; } = string.Empty;
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
}
