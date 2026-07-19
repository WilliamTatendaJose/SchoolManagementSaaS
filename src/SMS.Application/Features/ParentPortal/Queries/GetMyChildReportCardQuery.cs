using MediatR;
using SMS.Application.Common.Models;
using SMS.Application.Features.Academic.Queries;

namespace SMS.Application.Features.ParentPortal.Queries;

/// <summary>
/// Downloads the PDF report card for one of the signed-in parent's children. Parents
/// cannot supply comment overrides - only the persisted class-teacher/head remarks are
/// ever shown to them, never an ad-hoc value passed in the request.
/// </summary>
public record GetMyChildReportCardQuery : IRequest<Result<ReportCardFileDto>>
{
    public Guid StudentId { get; init; }
    public Guid AcademicTermId { get; init; }
    public string? GradingScheme { get; init; }
}
