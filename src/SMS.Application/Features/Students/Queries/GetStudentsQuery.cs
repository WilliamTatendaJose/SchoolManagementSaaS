using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Students.Queries;

/// <summary>
/// Query to get students with filtering and pagination
/// </summary>
public record GetStudentsQuery : IRequest<Result<PaginatedList<StudentDto>>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? SearchTerm { get; init; }
    public Guid? ClassId { get; init; }
    public string? Status { get; init; }
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; }
}

public record StudentDto
{
    public Guid Id { get; init; }
    public string StudentNumber { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string MiddleName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public DateTime DateOfBirth { get; init; }
    public string Gender { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? Photo { get; init; }
    public string? ClassName { get; init; }
    public string? HouseName { get; init; }
    public DateTime AdmissionDate { get; init; }
    public string? PrimaryGuardianName { get; init; }
    public string? PrimaryGuardianPhone { get; init; }
}
