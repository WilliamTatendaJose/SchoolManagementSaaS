using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Guardians.Commands;

/// <summary>
/// Command to update an existing guardian
/// </summary>
public record UpdateGuardianCommand : IRequest<Result>
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Gender { get; init; } = string.Empty;
    public string? NationalId { get; init; }
    public string? Phone { get; init; }
    public string? AlternatePhone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? Occupation { get; init; }
    public string? Employer { get; init; }
}
