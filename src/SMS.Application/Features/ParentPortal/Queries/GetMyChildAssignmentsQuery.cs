using MediatR;
using SMS.Application.Common.Models;
using SMS.Application.Features.Lms.Queries;

namespace SMS.Application.Features.ParentPortal.Queries;

/// <summary>Assignments for one of the caller's own children (ownership-scoped).</summary>
public record GetMyChildAssignmentsQuery(Guid StudentId) : IRequest<Result<List<StudentAssignmentDto>>>;
