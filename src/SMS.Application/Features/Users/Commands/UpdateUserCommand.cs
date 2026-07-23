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

    /// <summary>
    /// The guardian this login belongs to (for parent portal access), or null for none.
    /// The parent portal resolves a parent's children through <c>Guardian.UserId</c>, so
    /// this is what actually connects a login to its family. The edit form always sends
    /// the intended value, so null here means "not linked to any guardian".
    /// </summary>
    public Guid? GuardianId { get; init; }
}
