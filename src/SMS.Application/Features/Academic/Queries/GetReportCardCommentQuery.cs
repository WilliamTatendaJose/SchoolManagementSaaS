using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Academic.Queries;

/// <summary>Fetches the persisted report-card remarks for a student and term, if any were saved.</summary>
public record GetReportCardCommentQuery(Guid StudentId, Guid AcademicTermId) : IRequest<Result<ReportCardCommentDto>>;

public record ReportCardCommentDto
{
    public string? ClassTeacherComment { get; init; }
    public string? HeadComment { get; init; }
}
