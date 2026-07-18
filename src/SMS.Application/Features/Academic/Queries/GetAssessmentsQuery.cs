using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Academic.Queries;

public record GetAssessmentsQuery : IRequest<Result<List<AssessmentDto>>>
{
    public Guid? ClassId { get; init; }
    public Guid? SubjectId { get; init; }
    public Guid? AcademicTermId { get; init; }
    public string? AssessmentType { get; init; }
}

public record AssessmentDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid SubjectId { get; init; }
    public string SubjectName { get; init; } = string.Empty;
    public Guid ClassId { get; init; }
    public string ClassName { get; init; } = string.Empty;
    public Guid AcademicTermId { get; init; }
    public string TermName { get; init; } = string.Empty;
    public string AssessmentType { get; init; } = string.Empty;
    public decimal MaxScore { get; init; }
    public decimal WeightPercentage { get; init; }
    public DateTime? Date { get; init; }
    public bool IsPublished { get; init; }
    public int ResultCount { get; init; }
    public decimal? AverageScore { get; init; }
}
