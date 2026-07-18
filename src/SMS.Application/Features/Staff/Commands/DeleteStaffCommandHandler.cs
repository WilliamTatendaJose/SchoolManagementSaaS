using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Staff.Commands;

public class DeleteStaffCommandHandler : IRequestHandler<DeleteStaffCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public DeleteStaffCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(DeleteStaffCommand request, CancellationToken cancellationToken)
    {
        var staff = await _context.Staff
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (staff == null)
        {
            return Result.Failure("Staff member not found");
        }

        var isClassTeacher = await _context.Classes
            .AnyAsync(c => c.ClassTeacherId == request.Id, cancellationToken);
        var hasLeaveRequests = await _context.LeaveRequests
            .AnyAsync(l => l.StaffId == request.Id, cancellationToken);

        if (isClassTeacher || hasLeaveRequests)
        {
            return Result.Failure(
                "Cannot delete a staff member who is assigned as a class teacher or has leave request history. Reassign the class first.");
        }

        var subjectAssignments = await _context.TeacherSubjects
            .Where(ts => ts.StaffId == request.Id)
            .ToListAsync(cancellationToken);

        if (subjectAssignments.Count > 0)
        {
            _context.TeacherSubjects.RemoveRange(subjectAssignments);
        }

        _context.Staff.Remove(staff);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
