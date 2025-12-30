using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Users.Commands;

public record ResetPasswordCommand : IRequest<Result<string>>
{
    public Guid UserId { get; init; }
}
