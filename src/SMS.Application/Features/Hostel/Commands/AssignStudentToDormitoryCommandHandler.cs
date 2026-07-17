using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Hostel.Commands;

public class AssignStudentToDormitoryCommandHandler : IRequestHandler<AssignStudentToDormitoryCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public AssignStudentToDormitoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(AssignStudentToDormitoryCommand request, CancellationToken cancellationToken)
    {
        var student = await _context.Students.FirstOrDefaultAsync(s => s.Id == request.StudentId, cancellationToken);
        if (student == null)
        {
            return Result.Failure("Student not found");
        }

        // Un-assign
        if (request.DormitoryId is null)
        {
            student.DormitoryId = null;
            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        var dormitory = await _context.Dormitories
            .FirstOrDefaultAsync(d => d.Id == request.DormitoryId.Value, cancellationToken);

        if (dormitory == null)
        {
            return Result.Failure("Dormitory not found");
        }

        if (!dormitory.IsActive)
        {
            return Result.Failure("Dormitory is not active");
        }

        // Already there - nothing to do.
        if (student.DormitoryId == dormitory.Id)
        {
            return Result.Success();
        }

        if (!string.IsNullOrWhiteSpace(dormitory.Gender)
            && !string.Equals(dormitory.Gender, student.Gender.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure($"This dormitory is for {dormitory.Gender} students");
        }

        var occupants = await _context.Students
            .CountAsync(s => s.DormitoryId == dormitory.Id, cancellationToken);

        if (occupants >= dormitory.Capacity)
        {
            return Result.Failure("The dormitory is full");
        }

        student.DormitoryId = dormitory.Id;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
