using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Staff.Commands;

public class RemoveSubjectFromTeacherCommandHandler : IRequestHandler<RemoveSubjectFromTeacherCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public RemoveSubjectFromTeacherCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(RemoveSubjectFromTeacherCommand request, CancellationToken cancellationToken)
    {
        var teacherSubject = await _context.TeacherSubjects
            .FirstOrDefaultAsync(ts => ts.StaffId == request.StaffId && ts.SubjectId == request.SubjectId, cancellationToken);

        if (teacherSubject == null)
        {
            return Result.Failure("Subject is not assigned to this teacher");
        }

        _context.TeacherSubjects.Remove(teacherSubject);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
