using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Staff.Queries;

public record GetStaffQuery : IRequest<Result<PaginatedList<StaffListDto>>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? SearchTerm { get; init; }
    public bool? IsTeacher { get; init; }
    public bool? IsActive { get; init; }
    public string? Department { get; init; }
}

public record StaffListDto
{
    public Guid Id { get; init; }
    public string StaffNumber { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Department { get; init; }
    public string? JobTitle { get; init; }
    public bool IsTeacher { get; init; }
    public bool IsActive { get; init; }
}
