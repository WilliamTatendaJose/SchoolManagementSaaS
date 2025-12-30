using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Users.Commands;

public record UpdateUserCommand : IRequest<Result>
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public bool IsActive { get; init; }
}
