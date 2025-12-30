using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Academic.Queries;

public record GetStudentResultsQuery : IRequest<Result<StudentAcademicResultsDto>>
{
    public Guid StudentId { get; init; }
    public Guid? AcademicTermId { get; init; }
    public Guid? SubjectId { get; init; }
}

public record StudentAcademicResultsDto
{
    public Guid StudentId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public string StudentNumber { get; init; } = string.Empty;
    public string ClassName { get; init; } = string.Empty;
    public string? TermName { get; init; }
    public decimal OverallAverage { get; init; }
    public string OverallGrade { get; init; } = string.Empty;
    public int ClassRank { get; init; }
    public int TotalInClass { get; init; }
    public List<SubjectResultSummaryDto> SubjectResults { get; init; } = [];
}

public record SubjectResultSummaryDto
{
    public Guid SubjectId { get; init; }
    public string SubjectName { get; init; } = string.Empty;
    public decimal AverageScore { get; init; }
    public decimal AveragePercentage { get; init; }
    public string Grade { get; init; } = string.Empty;
    public List<AssessmentResultItemDto> Assessments { get; init; } = [];
}

public record AssessmentResultItemDto
{
    public Guid AssessmentId { get; init; }
    public string AssessmentName { get; init; } = string.Empty;
    public string AssessmentType { get; init; } = string.Empty;
    public decimal Score { get; init; }
    public decimal MaxScore { get; init; }
    public decimal Percentage { get; init; }
    public string Grade { get; init; } = string.Empty;
    public decimal WeightPercentage { get; init; }
}
