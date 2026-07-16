using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Guardians.Queries;

/// <summary>
/// Query to get guardians with filtering and pagination
/// </summary>
public record GetGuardiansQuery : IRequest<Result<PaginatedList<GuardianListDto>>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? SearchTerm { get; init; }
    public Guid? StudentId { get; init; }
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; }
}

public record GuardianListDto
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Gender { get; init; } = string.Empty;
    public string? NationalId { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Occupation { get; init; }
    public int StudentCount { get; init; }
}
