using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Users.Commands;

public record AssignRolesCommand : IRequest<Result>
{
    public Guid UserId { get; init; }
    public List<string> Roles { get; init; } = [];
}
