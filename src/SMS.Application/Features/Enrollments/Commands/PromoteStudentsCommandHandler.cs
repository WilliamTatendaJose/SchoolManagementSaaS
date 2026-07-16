using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;

namespace SMS.Application.Features.Enrollments.Commands;

public class PromoteStudentsCommandHandler : IRequestHandler<PromoteStudentsCommand, Result<PromotionResultDto>>
{
    private readonly IApplicationDbContext _context;

    public PromoteStudentsCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PromotionResultDto>> Handle(PromoteStudentsCommand request, CancellationToken cancellationToken)
    {
        if (!await _context.Classes.AnyAsync(c => c.Id == request.ToClassId, cancellationToken))
        {
            return Result<PromotionResultDto>.Failure("Target class not found");
        }

        if (!await _context.AcademicYears.AnyAsync(y => y.Id == request.ToAcademicYearId, cancellationToken))
        {
            return Result<PromotionResultDto>.Failure("Target academic year not found");
        }

        if (request.ToStreamId.HasValue &&
            !await _context.Streams.AnyAsync(s => s.Id == request.ToStreamId.Value && s.ClassId == request.ToClassId, cancellationToken))
        {
            return Result<PromotionResultDto>.Failure("Target stream not found for the selected class");
        }

        var requestedIds = request.StudentIds.Distinct().ToList();

        var students = await _context.Students
            .Where(s => requestedIds.Contains(s.Id))
            .ToListAsync(cancellationToken);

        var foundIds = students.Select(s => s.Id).ToHashSet();
        var skipped = requestedIds.Where(id => !foundIds.Contains(id)).ToList();

        // Students already enrolled in the target year are left untouched
        var alreadyInTargetYear = (await _context.Enrollments
            .Where(e => foundIds.Contains(e.StudentId) && e.AcademicYearId == request.ToAcademicYearId)
            .Select(e => e.StudentId)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        var studentsToPromote = students.Where(s => !alreadyInTargetYear.Contains(s.Id)).ToList();
        skipped.AddRange(alreadyInTargetYear);

        if (studentsToPromote.Count == 0)
        {
            return Result<PromotionResultDto>.Success(new PromotionResultDto
            {
                PromotedCount = 0,
                SkippedStudentIds = skipped
            });
        }

        var promoteIds = studentsToPromote.Select(s => s.Id).ToHashSet();

        // Deactivate existing active enrollments only for the students being promoted
        var activeEnrollments = await _context.Enrollments
            .Where(e => promoteIds.Contains(e.StudentId) && e.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var enrollment in activeEnrollments)
        {
            enrollment.IsActive = false;
        }

        var enrollmentDate = request.EnrollmentDate ?? DateTime.UtcNow;

        foreach (var student in studentsToPromote)
        {
            _context.Enrollments.Add(new Enrollment
            {
                StudentId = student.Id,
                ClassId = request.ToClassId,
                StreamId = request.ToStreamId,
                AcademicYearId = request.ToAcademicYearId,
                EnrollmentDate = enrollmentDate,
                IsActive = true
            });

            student.CurrentClassId = request.ToClassId;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result<PromotionResultDto>.Success(new PromotionResultDto
        {
            PromotedCount = studentsToPromote.Count,
            SkippedStudentIds = skipped
        });
    }
}
