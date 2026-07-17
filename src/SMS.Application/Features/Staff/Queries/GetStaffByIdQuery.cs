using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Staff.Queries;

public record GetStaffByIdQuery(Guid Id) : IRequest<Result<StaffDetailDto>>;

public record StaffDetailDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string StaffNumber { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Department { get; init; }
    public string? JobTitle { get; init; }
    public DateTime? DateOfJoining { get; init; }
    public string? Qualifications { get; init; }
    public string? Specialization { get; init; }
    public bool IsTeacher { get; init; }
    public bool IsActive { get; init; }
    public List<TeacherSubjectDto> Subjects { get; init; } = [];
    public int PendingLeaveRequests { get; init; }
}

public record TeacherSubjectDto
{
    public Guid SubjectId { get; init; }
    public string SubjectName { get; init; } = string.Empty;
    public bool IsPrimary { get; init; }
}
