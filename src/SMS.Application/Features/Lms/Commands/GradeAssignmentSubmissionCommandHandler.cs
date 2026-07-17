using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Interfaces;
using Result = SMS.Application.Common.Models.Result;

namespace SMS.Application.Features.Lms.Commands;

public class GradeAssignmentSubmissionCommandHandler : IRequestHandler<GradeAssignmentSubmissionCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public GradeAssignmentSubmissionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(GradeAssignmentSubmissionCommand request, CancellationToken cancellationToken)
    {
        var submission = await _context.AssignmentSubmissions
            .FirstOrDefaultAsync(s => s.Id == request.SubmissionId, cancellationToken);

        if (submission == null)
        {
            return Result.Failure("Submission not found");
        }

        submission.Grade = request.Grade;
        submission.Feedback = request.Feedback;
        submission.Status = SubmissionStatuses.Graded;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
