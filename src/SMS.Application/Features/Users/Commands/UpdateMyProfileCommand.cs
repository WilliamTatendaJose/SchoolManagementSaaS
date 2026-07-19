using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Users.Commands;

/// <summary>
/// Self-service profile update - deliberately a separate command from UpdateUserCommand
/// (the admin-only user editor), which also carries IsActive. A user updating their own
/// profile must never be able to touch their own active/inactive flag.
/// </summary>
public record UpdateMyProfileCommand : IRequest<Result>
{
    public Guid UserId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Phone { get; init; }
}
