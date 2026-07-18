using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Academic.Commands;

public class DeleteClassCommandHandler : IRequestHandler<DeleteClassCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public DeleteClassCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(DeleteClassCommand request, CancellationToken cancellationToken)
    {
        var classEntity = await _context.Classes
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (classEntity == null)
        {
            return Result.Failure("Class not found");
        }

        var hasStudents = await _context.Students
            .AnyAsync(s => s.CurrentClassId == request.Id, cancellationToken);
        var hasActiveEnrollments = await _context.Enrollments
            .AnyAsync(e => e.ClassId == request.Id && e.IsActive, cancellationToken);

        if (hasStudents || hasActiveEnrollments)
        {
            return Result.Failure("Cannot delete a class with enrolled students. Transfer or withdraw them first.");
        }

        _context.Classes.Remove(classEntity);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
