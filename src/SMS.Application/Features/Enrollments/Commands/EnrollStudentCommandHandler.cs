using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;

namespace SMS.Application.Features.Enrollments.Commands;

public class EnrollStudentCommandHandler : IRequestHandler<EnrollStudentCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public EnrollStudentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(EnrollStudentCommand request, CancellationToken cancellationToken)
    {
        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.Id == request.StudentId, cancellationToken);

        if (student == null)
        {
            return Result<Guid>.Failure("Student not found");
        }

        if (!await _context.Classes.AnyAsync(c => c.Id == request.ClassId, cancellationToken))
        {
            return Result<Guid>.Failure("Class not found");
        }

        if (!await _context.AcademicYears.AnyAsync(y => y.Id == request.AcademicYearId, cancellationToken))
        {
            return Result<Guid>.Failure("Academic year not found");
        }

        if (request.StreamId.HasValue &&
            !await _context.Streams.AnyAsync(s => s.Id == request.StreamId.Value && s.ClassId == request.ClassId, cancellationToken))
        {
            return Result<Guid>.Failure("Stream not found for the selected class");
        }

        var hasActiveEnrollment = await _context.Enrollments.AnyAsync(
            e => e.StudentId == request.StudentId && e.AcademicYearId == request.AcademicYearId && e.IsActive,
            cancellationToken);

        if (hasActiveEnrollment)
        {
            return Result<Guid>.Failure("Student already has an active enrollment for this academic year");
        }

        var enrollment = new Enrollment
        {
            StudentId = request.StudentId,
            ClassId = request.ClassId,
            StreamId = request.StreamId,
            AcademicYearId = request.AcademicYearId,
            EnrollmentDate = request.EnrollmentDate ?? DateTime.UtcNow,
            IsActive = true
        };

        _context.Enrollments.Add(enrollment);

        // Keep the student's current placement in sync
        student.CurrentClassId = request.ClassId;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(enrollment.Id);
    }
}
