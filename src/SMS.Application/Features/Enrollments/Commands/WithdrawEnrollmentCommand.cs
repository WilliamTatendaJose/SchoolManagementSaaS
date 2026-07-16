using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Enrollments.Commands;

/// <summary>
/// Command to withdraw (deactivate) a student enrollment
/// </summary>
public record WithdrawEnrollmentCommand(Guid EnrollmentId) : IRequest<Result>;
