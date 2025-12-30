using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Attendance.Queries;

public record GetClassAttendanceQuery : IRequest<Result<List<ClassAttendanceDto>>>
{
    public Guid ClassId { get; init; }
    public DateTime Date { get; init; }
    public Guid? SubjectId { get; init; }
}

public record ClassAttendanceDto
{
    public Guid Id { get; init; }
    public Guid StudentId { get; init; }
    public string StudentNumber { get; init; } = string.Empty;
    public string StudentName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public TimeOnly? TimeIn { get; init; }
    public string? Reason { get; init; }
    public bool SmsNotificationSent { get; init; }
}
