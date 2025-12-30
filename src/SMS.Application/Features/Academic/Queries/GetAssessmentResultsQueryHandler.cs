using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Academic.Queries;

public class GetAssessmentResultsQueryHandler : IRequestHandler<GetAssessmentResultsQuery, Result<AssessmentResultsDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAssessmentResultsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<AssessmentResultsDto>> Handle(GetAssessmentResultsQuery request, CancellationToken cancellationToken)
    {
        var assessment = await _context.Assessments
            .AsNoTracking()
            .Include(a => a.Subject)
            .Include(a => a.Class)
            .Include(a => a.Results)
                .ThenInclude(r => r.Student)
            .FirstOrDefaultAsync(a => a.Id == request.AssessmentId, cancellationToken);

        if (assessment == null)
        {
            return Result<AssessmentResultsDto>.Failure("Assessment not found");
        }

        // Get total students in class
        var totalStudents = await _context.Students
            .CountAsync(s => s.CurrentClassId == assessment.ClassId, cancellationToken);

        // Calculate statistics
        var results = assessment.Results.ToList();
        var passThreshold = assessment.MaxScore * 0.5m; // 50% pass mark

        var statistics = new AssessmentStatisticsDto
        {
            TotalStudents = totalStudents,
            ResultsRecorded = results.Count,
            AverageScore = results.Any() ? Math.Round(results.Average(r => r.Score), 2) : null,
            AveragePercentage = results.Any() && assessment.MaxScore > 0 
                ? Math.Round(results.Average(r => r.Score) / assessment.MaxScore * 100, 2) 
                : null,
            HighestScore = results.Any() ? results.Max(r => r.Score) : null,
            LowestScore = results.Any() ? results.Min(r => r.Score) : null,
            PassCount = results.Count(r => r.Score >= passThreshold),
            FailCount = results.Count(r => r.Score < passThreshold),
            PassRate = results.Any() 
                ? Math.Round((decimal)results.Count(r => r.Score >= passThreshold) / results.Count * 100, 2) 
                : 0
        };

        // Build ranked results
        var rankedResults = results
            .OrderByDescending(r => r.Score)
            .Select((r, index) => new StudentResultDetailDto
            {
                ResultId = r.Id,
                StudentId = r.StudentId,
                StudentNumber = r.Student.StudentNumber,
                StudentName = r.Student.FullName,
                Score = r.Score,
                Percentage = assessment.MaxScore > 0 
                    ? Math.Round(r.Score / assessment.MaxScore * 100, 2) 
                    : 0,
                Grade = r.Grade ?? "N/A",
                Comment = r.Comment,
                Rank = index + 1
            })
            .ToList();

        var dto = new AssessmentResultsDto
        {
            AssessmentId = assessment.Id,
            AssessmentName = assessment.Name,
            SubjectName = assessment.Subject.Name,
            ClassName = assessment.Class.Name,
            MaxScore = assessment.MaxScore,
            IsPublished = assessment.IsPublished,
            Statistics = statistics,
            Results = rankedResults
        };

        return Result<AssessmentResultsDto>.Success(dto);
    }
}
