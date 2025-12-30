using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Academic.Queries;

public record GetAssessmentResultsQuery : IRequest<Result<AssessmentResultsDto>>
{
    public Guid AssessmentId { get; init; }
}

public record AssessmentResultsDto
{
    public Guid AssessmentId { get; init; }
    public string AssessmentName { get; init; } = string.Empty;
    public string SubjectName { get; init; } = string.Empty;
    public string ClassName { get; init; } = string.Empty;
    public decimal MaxScore { get; init; }
    public bool IsPublished { get; init; }
    public AssessmentStatisticsDto Statistics { get; init; } = new();
    public List<StudentResultDetailDto> Results { get; init; } = [];
}

public record AssessmentStatisticsDto
{
    public int TotalStudents { get; init; }
    public int ResultsRecorded { get; init; }
    public decimal? AverageScore { get; init; }
    public decimal? AveragePercentage { get; init; }
    public decimal? HighestScore { get; init; }
    public decimal? LowestScore { get; init; }
    public int PassCount { get; init; }
    public int FailCount { get; init; }
    public decimal PassRate { get; init; }
}

public record StudentResultDetailDto
{
    public Guid ResultId { get; init; }
    public Guid StudentId { get; init; }
    public string StudentNumber { get; init; } = string.Empty;
    public string StudentName { get; init; } = string.Empty;
    public decimal Score { get; init; }
    public decimal Percentage { get; init; }
    public string Grade { get; init; } = string.Empty;
    public string? Comment { get; init; }
    public int Rank { get; init; }
}
