using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Attendance.Commands;

public record MarkAttendanceCommand : IRequest<Result<int>>
{
    public Guid ClassId { get; init; }
    public Guid? SubjectId { get; init; }
    public Guid? TimetableSlotId { get; init; }
    public DateTime Date { get; init; }
    public bool SendNotifications { get; init; }
    public List<AttendanceRecordDto> Records { get; init; } = [];
}

public record AttendanceRecordDto
{
    public Guid StudentId { get; init; }
    public string Status { get; init; } = string.Empty;
    public TimeOnly? TimeIn { get; init; }
    public string? Reason { get; init; }
}
