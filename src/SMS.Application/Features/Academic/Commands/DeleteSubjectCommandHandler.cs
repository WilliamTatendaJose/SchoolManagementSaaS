using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Academic.Commands;

public class DeleteSubjectCommandHandler : IRequestHandler<DeleteSubjectCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public DeleteSubjectCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(DeleteSubjectCommand request, CancellationToken cancellationToken)
    {
        var subject = await _context.Subjects
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (subject == null)
        {
            return Result.Failure("Subject not found");
        }

        var isAssignedToTeacher = await _context.TeacherSubjects
            .AnyAsync(ts => ts.SubjectId == request.Id, cancellationToken);
        var hasAssessments = await _context.Assessments
            .AnyAsync(a => a.SubjectId == request.Id, cancellationToken);

        if (isAssignedToTeacher || hasAssessments)
        {
            return Result.Failure("Cannot delete a subject that is assigned to teachers or has assessments recorded.");
        }

        _context.Subjects.Remove(subject);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
