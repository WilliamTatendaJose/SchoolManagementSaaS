using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Guardians.Commands;

/// <summary>
/// Command to remove the association between a guardian and a student
/// </summary>
public record UnlinkGuardianFromStudentCommand(Guid GuardianId, Guid StudentId) : IRequest<Result>;
