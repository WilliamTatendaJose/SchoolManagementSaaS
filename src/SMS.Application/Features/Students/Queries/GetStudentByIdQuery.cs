using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Students.Queries;

/// <summary>
/// Query to get a single student by ID
/// </summary>
public record GetStudentByIdQuery(Guid Id) : IRequest<Result<StudentDetailDto>>;

public record StudentDetailDto
{
    public Guid Id { get; init; }
    public string StudentNumber { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string MiddleName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public DateTime DateOfBirth { get; init; }
    public string Gender { get; init; } = string.Empty;
    public string? NationalId { get; init; }
    public string? BirthCertificateNumber { get; init; }
    public string? Photo { get; init; }
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? Nationality { get; init; }
    public string? Religion { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime AdmissionDate { get; init; }
    public DateTime? GraduationDate { get; init; }
    public string? MedicalNotes { get; init; }
    public string? SpecialNeeds { get; init; }
    public string? PreviousSchool { get; init; }
    
    // Current placement
    public Guid? CurrentClassId { get; init; }
    public string? CurrentClassName { get; init; }
    public Guid? HouseId { get; init; }
    public string? HouseName { get; init; }
    public Guid? DormitoryId { get; init; }
    public string? DormitoryName { get; init; }
    
    // Related data
    public List<GuardianDto> Guardians { get; init; } = [];
    public decimal OutstandingBalance { get; init; }
    public int AttendancePercentage { get; init; }
    public int TotalDemerits { get; init; }
    public int TotalMerits { get; init; }
}

public record GuardianDto
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string Relationship { get; init; } = string.Empty;
    public bool IsPrimaryContact { get; init; }
    public bool IsEmergencyContact { get; init; }
}
