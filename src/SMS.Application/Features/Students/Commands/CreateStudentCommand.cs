using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Students.Commands;

/// <summary>
/// Command to create a new student
/// </summary>
public record CreateStudentCommand : IRequest<Result<Guid>>
{
    public string FirstName { get; init; } = string.Empty;
    public string MiddleName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public DateTime DateOfBirth { get; init; }
    public string Gender { get; init; } = string.Empty;
    public string? NationalId { get; init; }
    public string? BirthCertificateNumber { get; init; }
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? Nationality { get; init; }
    public string? Religion { get; init; }
    public string? MedicalNotes { get; init; }
    public string? SpecialNeeds { get; init; }
    public string? PreviousSchool { get; init; }
    public Guid? ClassId { get; init; }
    public Guid? HouseId { get; init; }
    public DateTime AdmissionDate { get; init; }
    public List<GuardianInfo>? Guardians { get; init; }
}

public record GuardianInfo
{
    public Guid? GuardianId { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string Relationship { get; init; } = string.Empty;
    public bool IsPrimaryContact { get; init; }
    public bool IsEmergencyContact { get; init; }
}
