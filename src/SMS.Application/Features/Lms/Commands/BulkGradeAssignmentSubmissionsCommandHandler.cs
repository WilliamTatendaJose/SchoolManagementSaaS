using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using Result = SMS.Application.Common.Models.Result;

namespace SMS.Application.Features.Lms.Commands;

public class BulkGradeAssignmentSubmissionsCommandHandler : IRequestHandler<BulkGradeAssignmentSubmissionsCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public BulkGradeAssignmentSubmissionsCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(BulkGradeAssignmentSubmissionsCommand request, CancellationToken cancellationToken)
    {
        var assignment = await _context.Assignments
            .FirstOrDefaultAsync(a => a.Id == request.AssignmentId, cancellationToken);
        if (assignment == null)
        {
            return Result.Failure("Assignment not found");
        }

        var entries = request.Grades.Where(g => g.Grade.HasValue).ToList();
        if (entries.Count == 0)
        {
            return Result.Failure("No grades to save");
        }

        var studentIds = entries.Select(e => e.StudentId).ToHashSet();

        // Only students actually in the assignment's class may be graded, so a stale or
        // hand-crafted request can't attach grades to arbitrary students.
        var validStudentIds = (await _context.Students
            .Where(s => studentIds.Contains(s.Id) && s.CurrentClassId == assignment.ClassId)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        var existing = await _context.AssignmentSubmissions
            .Where(s => s.AssignmentId == request.AssignmentId && studentIds.Contains(s.StudentId))
            .ToListAsync(cancellationToken);
        var existingByStudent = existing.ToDictionary(s => s.StudentId);

        var graded = 0;
        foreach (var entry in entries)
        {
            if (!validStudentIds.Contains(entry.StudentId))
            {
                continue;
            }

            if (existingByStudent.TryGetValue(entry.StudentId, out var submission))
            {
                submission.Grade = entry.Grade;
                submission.Feedback = entry.Feedback;
                submission.Status = SubmissionStatuses.Graded;
            }
            else
            {
                // No digital submission - the student handed in on paper. Record a graded
                // submission so the mark is captured against the roster.
                _context.AssignmentSubmissions.Add(new AssignmentSubmission
                {
                    AssignmentId = request.AssignmentId,
                    StudentId = entry.StudentId,
                    SubmittedAt = DateTime.UtcNow,
                    Grade = entry.Grade,
                    Feedback = entry.Feedback,
                    Status = SubmissionStatuses.Graded
                });
            }

            graded++;
        }

        if (graded == 0)
        {
            return Result.Failure("None of the students are in this assignment's class");
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
