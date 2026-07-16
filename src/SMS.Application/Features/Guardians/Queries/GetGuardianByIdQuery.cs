using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Guardians.Queries;

/// <summary>
/// Query to get a single guardian by ID, including linked students
/// </summary>
public record GetGuardianByIdQuery(Guid Id) : IRequest<Result<GuardianDetailDto>>;

public record GuardianDetailDto
{
    public Guid Id { get; init; }
    public Guid? UserId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Gender { get; init; } = string.Empty;
    public string? NationalId { get; init; }
    public string? Phone { get; init; }
    public string? AlternatePhone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? Occupation { get; init; }
    public string? Employer { get; init; }

    public List<LinkedStudentDto> Students { get; init; } = [];
}

public record LinkedStudentDto
{
    public Guid StudentId { get; init; }
    public string StudentNumber { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? ClassName { get; init; }
    public string Relationship { get; init; } = string.Empty;
    public bool IsPrimaryContact { get; init; }
    public bool IsEmergencyContact { get; init; }
    public bool CanPickup { get; init; }
}
