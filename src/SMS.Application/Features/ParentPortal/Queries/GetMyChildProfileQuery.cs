using MediatR;
using SMS.Application.Common.Models;
using SMS.Application.Features.Students.Queries;

namespace SMS.Application.Features.ParentPortal.Queries;

/// <summary>Full profile of one of the caller's own children (ownership-scoped).</summary>
public record GetMyChildProfileQuery(Guid StudentId) : IRequest<Result<StudentDetailDto>>;
