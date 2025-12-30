using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Academic.Queries;

public class GetStudentResultsQueryHandler : IRequestHandler<GetStudentResultsQuery, Result<StudentAcademicResultsDto>>
{
    private readonly IApplicationDbContext _context;

    public GetStudentResultsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<StudentAcademicResultsDto>> Handle(GetStudentResultsQuery request, CancellationToken cancellationToken)
    {
        var student = await _context.Students
            .AsNoTracking()
            .Include(s => s.CurrentClass)
            .FirstOrDefaultAsync(s => s.Id == request.StudentId, cancellationToken);

        if (student == null)
        {
            return Result<StudentAcademicResultsDto>.Failure("Student not found");
        }

        // Get results for the student
        var resultsQuery = _context.Results
            .AsNoTracking()
            .Include(r => r.Assessment)
                .ThenInclude(a => a.Subject)
            .Include(r => r.Assessment)
                .ThenInclude(a => a.AcademicTerm)
            .Where(r => r.StudentId == request.StudentId && r.Assessment.IsPublished);

        if (request.AcademicTermId.HasValue)
        {
            resultsQuery = resultsQuery.Where(r => r.Assessment.AcademicTermId == request.AcademicTermId.Value);
        }

        if (request.SubjectId.HasValue)
        {
            resultsQuery = resultsQuery.Where(r => r.Assessment.SubjectId == request.SubjectId.Value);
        }

        var results = await resultsQuery.ToListAsync(cancellationToken);

        // Group by subject
        var subjectResults = results
            .GroupBy(r => r.Assessment.Subject)
            .Select(g =>
            {
                var subjectAssessments = g.ToList();
                var totalWeight = subjectAssessments.Sum(r => r.Assessment.WeightPercentage);
                
                // Calculate weighted average
                var weightedSum = subjectAssessments.Sum(r => 
                    (r.Score / r.Assessment.MaxScore) * r.Assessment.WeightPercentage);
                
                var avgPercentage = totalWeight > 0 ? (weightedSum / totalWeight) * 100 : 0;

                return new SubjectResultSummaryDto
                {
                    SubjectId = g.Key.Id,
                    SubjectName = g.Key.Name,
                    AverageScore = subjectAssessments.Average(r => r.Score),
                    AveragePercentage = Math.Round(avgPercentage, 2),
                    Grade = CalculateGrade(avgPercentage),
                    Assessments = subjectAssessments.Select(r => new AssessmentResultItemDto
                    {
                        AssessmentId = r.AssessmentId,
                        AssessmentName = r.Assessment.Name,
                        AssessmentType = r.Assessment.AssessmentType,
                        Score = r.Score,
                        MaxScore = r.Assessment.MaxScore,
                        Percentage = r.Assessment.MaxScore > 0 
                            ? Math.Round(r.Score / r.Assessment.MaxScore * 100, 2) 
                            : 0,
                        Grade = r.Grade ?? "N/A",
                        WeightPercentage = r.Assessment.WeightPercentage
                    }).OrderBy(a => a.AssessmentType).ThenBy(a => a.AssessmentName).ToList()
                };
            })
            .OrderBy(s => s.SubjectName)
            .ToList();

        // Calculate overall average
        var overallAverage = subjectResults.Any() 
            ? Math.Round(subjectResults.Average(s => s.AveragePercentage), 2) 
            : 0;

        // Get class rank (simplified - based on current term if specified)
        var (classRank, totalInClass) = await CalculateClassRankAsync(
            request.StudentId, 
            student.CurrentClassId, 
            request.AcademicTermId, 
            cancellationToken);

        // Get term name
        string? termName = null;
        if (request.AcademicTermId.HasValue)
        {
            var term = await _context.AcademicTerms
                .FirstOrDefaultAsync(t => t.Id == request.AcademicTermId.Value, cancellationToken);
            termName = term?.Name;
        }

        var dto = new StudentAcademicResultsDto
        {
            StudentId = student.Id,
            StudentName = student.FullName,
            StudentNumber = student.StudentNumber,
            ClassName = student.CurrentClass?.Name ?? "N/A",
            TermName = termName,
            OverallAverage = overallAverage,
            OverallGrade = CalculateGrade(overallAverage),
            ClassRank = classRank,
            TotalInClass = totalInClass,
            SubjectResults = subjectResults
        };

        return Result<StudentAcademicResultsDto>.Success(dto);
    }

    private async Task<(int rank, int total)> CalculateClassRankAsync(
        Guid studentId, 
        Guid? classId, 
        Guid? termId, 
        CancellationToken cancellationToken)
    {
        if (!classId.HasValue)
        {
            return (0, 0);
        }

        // Get all students in the class with their averages
        var classStudents = await _context.Students
            .AsNoTracking()
            .Where(s => s.CurrentClassId == classId)
            .Select(s => new
            {
                s.Id,
                AveragePercentage = _context.Results
                    .Where(r => r.StudentId == s.Id && r.Assessment.IsPublished)
                    .Where(r => !termId.HasValue || r.Assessment.AcademicTermId == termId.Value)
                    .Average(r => (decimal?)r.Score / r.Assessment.MaxScore * 100) ?? 0
            })
            .OrderByDescending(s => s.AveragePercentage)
            .ToListAsync(cancellationToken);

        var total = classStudents.Count;
        var rank = classStudents.FindIndex(s => s.Id == studentId) + 1;

        return (rank, total);
    }

    private static string CalculateGrade(decimal percentage)
    {
        return percentage switch
        {
            >= 90 => "A*",
            >= 80 => "A",
            >= 70 => "B",
            >= 60 => "C",
            >= 50 => "D",
            >= 40 => "E",
            _ => "U"
        };
    }
}
