using MediatR;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;

namespace SMS.Application.Features.Academic.Commands;

public class CreateAssessmentCommandHandler : IRequestHandler<CreateAssessmentCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateAssessmentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateAssessmentCommand request, CancellationToken cancellationToken)
    {
        var assessment = new Assessment
        {
            Name = request.Name,
            Description = request.Description,
            SubjectId = request.SubjectId,
            ClassId = request.ClassId,
            AcademicTermId = request.AcademicTermId,
            AssessmentType = request.AssessmentType,
            MaxScore = request.MaxScore,
            WeightPercentage = request.WeightPercentage,
            Date = request.Date,
            IsPublished = false
        };

        _context.Assessments.Add(assessment);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(assessment.Id);
    }
}
