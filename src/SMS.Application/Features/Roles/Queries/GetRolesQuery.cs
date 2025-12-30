using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Roles.Queries;

public record GetRolesQuery : IRequest<Result<List<RoleDetailDto>>>;

public record RoleDetailDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsSystemRole { get; init; }
    public int UserCount { get; init; }
    public List<PermissionDto> Permissions { get; init; } = [];
}

public record PermissionDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Module { get; init; } = string.Empty;
}
