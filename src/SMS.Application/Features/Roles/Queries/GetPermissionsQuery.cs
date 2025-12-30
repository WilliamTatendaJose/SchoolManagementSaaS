using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Roles.Queries;

public record GetPermissionsQuery : IRequest<Result<List<PermissionGroupDto>>>
{
    public string? Module { get; init; }
}

public record PermissionGroupDto
{
    public string Module { get; init; } = string.Empty;
    public List<PermissionDto> Permissions { get; init; } = [];
}
