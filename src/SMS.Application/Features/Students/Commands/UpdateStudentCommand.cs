using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Students.Commands;

public record UpdateStudentCommand : IRequest<Result>
{
    public Guid Id { get; init; }
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
    public Guid? ClassId { get; init; }
    public Guid? HouseId { get; init; }
    public Guid? DormitoryId { get; init; }
}
