using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Academic.Queries;

public class GetAssessmentsQueryHandler : IRequestHandler<GetAssessmentsQuery, Result<List<AssessmentDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetAssessmentsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<AssessmentDto>>> Handle(GetAssessmentsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Assessments
            .AsNoTracking()
            .Include(a => a.Subject)
            .Include(a => a.Class)
            .Include(a => a.AcademicTerm)
            .Include(a => a.Results)
            .AsQueryable();

        if (request.ClassId.HasValue)
        {
            query = query.Where(a => a.ClassId == request.ClassId.Value);
        }

        if (request.SubjectId.HasValue)
        {
            query = query.Where(a => a.SubjectId == request.SubjectId.Value);
        }

        if (request.AcademicTermId.HasValue)
        {
            query = query.Where(a => a.AcademicTermId == request.AcademicTermId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.AssessmentType))
        {
            query = query.Where(a => a.AssessmentType == request.AssessmentType);
        }

        var assessments = await query
            .OrderByDescending(a => a.Date)
            .ThenBy(a => a.Name)
            .Select(a => new AssessmentDto
            {
                Id = a.Id,
                Name = a.Name,
                Description = a.Description,
                SubjectId = a.SubjectId,
                SubjectName = a.Subject.Name,
                ClassId = a.ClassId,
                ClassName = a.Class.Name,
                AcademicTermId = a.AcademicTermId,
                TermName = a.AcademicTerm.Name,
                AssessmentType = a.AssessmentType,
                MaxScore = a.MaxScore,
                WeightPercentage = a.WeightPercentage,
                Date = a.Date,
                IsPublished = a.IsPublished,
                ResultCount = a.Results.Count,
                AverageScore = a.Results.Any() ? a.Results.Average(r => r.Score) : null
            })
            .ToListAsync(cancellationToken);

        return Result<List<AssessmentDto>>.Success(assessments);
    }
}
