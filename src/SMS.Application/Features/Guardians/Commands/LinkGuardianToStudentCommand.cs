using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Guardians.Commands;

/// <summary>
/// Command to link an existing guardian to an existing student
/// </summary>
public record LinkGuardianToStudentCommand : IRequest<Result<Guid>>
{
    public Guid GuardianId { get; init; }
    public Guid StudentId { get; init; }
    public string Relationship { get; init; } = string.Empty;
    public bool IsPrimaryContact { get; init; }
    public bool IsEmergencyContact { get; init; }
    public bool CanPickup { get; init; } = true;
}
