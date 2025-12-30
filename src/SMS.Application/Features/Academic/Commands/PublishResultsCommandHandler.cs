using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Academic.Commands;

public class PublishResultsCommandHandler : IRequestHandler<PublishResultsCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public PublishResultsCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(PublishResultsCommand request, CancellationToken cancellationToken)
    {
        var assessment = await _context.Assessments
            .FirstOrDefaultAsync(a => a.Id == request.AssessmentId, cancellationToken);

        if (assessment == null)
        {
            return Result.Failure("Assessment not found");
        }

        if (assessment.IsPublished)
        {
            return Result.Failure("Results are already published");
        }

        assessment.IsPublished = true;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
