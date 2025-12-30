using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Roles.Commands;

public record CreateRoleCommand : IRequest<Result<Guid>>
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public List<string> PermissionCodes { get; init; } = [];
}
