using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using ResultEntity = SMS.Domain.Entities.Result;

namespace SMS.Application.Features.Academic.Commands;

public class RecordResultsCommandHandler : IRequestHandler<RecordResultsCommand, Result<int>>
{
    private readonly IApplicationDbContext _context;

    public RecordResultsCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<int>> Handle(RecordResultsCommand request, CancellationToken cancellationToken)
    {
        var assessment = await _context.Assessments
            .FirstOrDefaultAsync(a => a.Id == request.AssessmentId, cancellationToken);

        if (assessment == null)
        {
            return Result<int>.Failure("Assessment not found");
        }

        var count = 0;

        foreach (var resultDto in request.Results)
        {
            // Check if result already exists
            var existingResult = await _context.Results
                .FirstOrDefaultAsync(r => 
                    r.AssessmentId == request.AssessmentId && 
                    r.StudentId == resultDto.StudentId, 
                    cancellationToken);

            if (existingResult != null)
            {
                // Update existing result
                existingResult.Score = resultDto.Score;
                existingResult.Comment = resultDto.Comment;
                existingResult.Grade = CalculateGrade(resultDto.Score, assessment.MaxScore);
            }
            else
            {
                // Create new result
                var result = new ResultEntity
                {
                    StudentId = resultDto.StudentId,
                    AssessmentId = request.AssessmentId,
                    Score = resultDto.Score,
                    Comment = resultDto.Comment,
                    Grade = CalculateGrade(resultDto.Score, assessment.MaxScore)
                };

                _context.Results.Add(result);
            }

            count++;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(count);
    }

    private static string CalculateGrade(decimal score, decimal maxScore)
    {
        if (maxScore == 0) return "N/A";

        var percentage = (score / maxScore) * 100;

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
