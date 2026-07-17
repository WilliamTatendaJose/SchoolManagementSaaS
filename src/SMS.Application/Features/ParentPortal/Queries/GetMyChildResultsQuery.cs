using MediatR;
using SMS.Application.Common.Models;
using SMS.Application.Features.Academic.Queries;

namespace SMS.Application.Features.ParentPortal.Queries;

/// <summary>Published results for one of the signed-in parent's children.</summary>
public record GetMyChildResultsQuery : IRequest<Result<StudentAcademicResultsDto>>
{
    public Guid StudentId { get; init; }
    public Guid? AcademicTermId { get; init; }
}
