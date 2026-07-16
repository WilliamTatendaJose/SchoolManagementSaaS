using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Enrollments.Queries;

/// <summary>
/// Query to get the full enrollment history for a single student
/// </summary>
public record GetStudentEnrollmentsQuery(Guid StudentId) : IRequest<Result<List<EnrollmentDto>>>;
