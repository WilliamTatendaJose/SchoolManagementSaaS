using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Users.Commands;

public record ChangePasswordCommand : IRequest<Result>
{
    public Guid UserId { get; init; }
    public string CurrentPassword { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
}
