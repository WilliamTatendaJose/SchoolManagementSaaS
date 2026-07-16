using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Enrollments.Commands;

public class TransferStudentCommandHandler : IRequestHandler<TransferStudentCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public TransferStudentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(TransferStudentCommand request, CancellationToken cancellationToken)
    {
        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(
                e => e.StudentId == request.StudentId && e.AcademicYearId == request.AcademicYearId && e.IsActive,
                cancellationToken);

        if (enrollment == null)
        {
            return Result.Failure("No active enrollment found for this student in the selected academic year");
        }

        if (!await _context.Classes.AnyAsync(c => c.Id == request.ToClassId, cancellationToken))
        {
            return Result.Failure("Target class not found");
        }

        if (request.ToStreamId.HasValue &&
            !await _context.Streams.AnyAsync(s => s.Id == request.ToStreamId.Value && s.ClassId == request.ToClassId, cancellationToken))
        {
            return Result.Failure("Target stream not found for the selected class");
        }

        if (enrollment.ClassId == request.ToClassId && enrollment.StreamId == request.ToStreamId)
        {
            return Result.Failure("Student is already enrolled in the selected class and stream");
        }

        enrollment.ClassId = request.ToClassId;
        enrollment.StreamId = request.ToStreamId;

        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.Id == request.StudentId, cancellationToken);

        if (student != null)
        {
            student.CurrentClassId = request.ToClassId;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
