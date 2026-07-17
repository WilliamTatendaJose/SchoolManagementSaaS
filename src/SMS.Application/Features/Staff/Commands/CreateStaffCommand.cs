using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Staff.Commands;

/// <summary>
/// Creates a staff profile for an existing user account.
/// </summary>
public record CreateStaffCommand : IRequest<Result<Guid>>
{
    public Guid UserId { get; init; }
    public string? Department { get; init; }
    public string? JobTitle { get; init; }
    public DateTime? DateOfJoining { get; init; }
    public string? Qualifications { get; init; }
    public string? Specialization { get; init; }
    public bool IsTeacher { get; init; }
}
