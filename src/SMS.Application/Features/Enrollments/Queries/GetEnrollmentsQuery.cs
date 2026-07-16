using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Enrollments.Queries;

/// <summary>
/// Query to get enrollments with filtering and pagination
/// </summary>
public record GetEnrollmentsQuery : IRequest<Result<PaginatedList<EnrollmentDto>>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public Guid? AcademicYearId { get; init; }
    public Guid? ClassId { get; init; }
    public Guid? StreamId { get; init; }
    public Guid? StudentId { get; init; }
    public bool? IsActive { get; init; }
    public string? SearchTerm { get; init; }
}

public record EnrollmentDto
{
    public Guid Id { get; init; }
    public Guid StudentId { get; init; }
    public string StudentNumber { get; init; } = string.Empty;
    public string StudentName { get; init; } = string.Empty;
    public Guid ClassId { get; init; }
    public string ClassName { get; init; } = string.Empty;
    public Guid? StreamId { get; init; }
    public string? StreamName { get; init; }
    public Guid AcademicYearId { get; init; }
    public string AcademicYearName { get; init; } = string.Empty;
    public DateTime EnrollmentDate { get; init; }
    public bool IsActive { get; init; }
}
