using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Academic.Queries;

/// <summary>
/// Produces a PDF report card for a student and term using the chosen grading scheme.
/// </summary>
public record GenerateReportCardQuery : IRequest<Result<ReportCardFileDto>>
{
    public Guid StudentId { get; init; }
    public Guid AcademicTermId { get; init; }
    public string? GradingScheme { get; init; }
    public string? ClassTeacherComment { get; init; }
    public string? HeadComment { get; init; }
}

public record ReportCardFileDto
{
    public string FileName { get; init; } = string.Empty;
    public byte[] Content { get; init; } = [];
}
