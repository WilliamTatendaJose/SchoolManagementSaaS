using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Staff.Commands;

/// <summary>
/// Allocates a subject to a teaching staff member.
/// </summary>
public record AssignSubjectToTeacherCommand : IRequest<Result<Guid>>
{
    public Guid StaffId { get; init; }
    public Guid SubjectId { get; init; }
    public bool IsPrimary { get; init; }
}
