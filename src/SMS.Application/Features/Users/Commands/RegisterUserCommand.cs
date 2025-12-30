using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Users.Commands;

public record RegisterUserCommand : IRequest<Result<Guid>>
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public List<string> Roles { get; init; } = [];
    public Guid? StaffId { get; init; }
    public Guid? GuardianId { get; init; }
}
