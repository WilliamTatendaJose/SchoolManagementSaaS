using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;

namespace SMS.Application.Features.Staff.Commands;

public class AssignSubjectToTeacherCommandHandler : IRequestHandler<AssignSubjectToTeacherCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public AssignSubjectToTeacherCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(AssignSubjectToTeacherCommand request, CancellationToken cancellationToken)
    {
        var staff = await _context.Staff.FirstOrDefaultAsync(s => s.Id == request.StaffId, cancellationToken);
        if (staff == null)
        {
            return Result<Guid>.Failure("Staff not found");
        }

        if (!staff.IsTeacher)
        {
            return Result<Guid>.Failure("Subjects can only be assigned to teaching staff");
        }

        if (!await _context.Subjects.AnyAsync(s => s.Id == request.SubjectId, cancellationToken))
        {
            return Result<Guid>.Failure("Subject not found");
        }

        if (await _context.TeacherSubjects.AnyAsync(
                ts => ts.StaffId == request.StaffId && ts.SubjectId == request.SubjectId, cancellationToken))
        {
            return Result<Guid>.Failure("This subject is already assigned to the teacher");
        }

        var teacherSubject = new TeacherSubject
        {
            StaffId = request.StaffId,
            SubjectId = request.SubjectId,
            IsPrimary = request.IsPrimary
        };

        _context.TeacherSubjects.Add(teacherSubject);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(teacherSubject.Id);
    }
}
