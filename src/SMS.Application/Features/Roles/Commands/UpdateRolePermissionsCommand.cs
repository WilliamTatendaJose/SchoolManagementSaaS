using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Roles.Commands;

public record UpdateRolePermissionsCommand : IRequest<Result>
{
    public Guid RoleId { get; init; }
    public List<string> PermissionCodes { get; init; } = [];
}
