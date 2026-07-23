using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Enums;

namespace SMS.Application.Features.Lms.Queries;

public class GetAssignmentRosterQueryHandler : IRequestHandler<GetAssignmentRosterQuery, Result<AssignmentRosterDto>>
{
    private readonly IApplicationDbContext _context;
    // Resolved lazily (not constructor-injected) so storage is only touched when a file
    // download URL is actually needed. File storage is DB-backed (no external dependency).
    private readonly IServiceProvider _serviceProvider;

    public GetAssignmentRosterQueryHandler(IApplicationDbContext context, IServiceProvider serviceProvider)
    {
        _context = context;
        _serviceProvider = serviceProvider;
    }

    public async Task<Result<AssignmentRosterDto>> Handle(GetAssignmentRosterQuery request, CancellationToken cancellationToken)
    {
        var assignment = await _context.Assignments
            .AsNoTracking()
            .Include(a => a.Class)
            .Include(a => a.Subject)
            .FirstOrDefaultAsync(a => a.Id == request.AssignmentId, cancellationToken);

        if (assignment == null)
        {
            return Result<AssignmentRosterDto>.Failure("Assignment not found");
        }

        // Every active student currently placed in the class is "on the roster" for this
        // assignment, whether or not they've submitted.
        var roster = await _context.Students
            .AsNoTracking()
            .Where(s => s.CurrentClassId == assignment.ClassId && s.Status == StudentStatus.Active)
            .Select(s => new { s.Id, s.StudentNumber, Name = s.FirstName + " " + s.LastName })
            .ToListAsync(cancellationToken);

        var submissions = await _context.AssignmentSubmissions
            .AsNoTracking()
            .Where(s => s.AssignmentId == request.AssignmentId)
            .Select(s => new
            {
                s.Id,
                s.StudentId,
                s.SubmittedAt,
                s.Comment,
                s.Feedback,
                s.Grade,
                s.AttachmentKey,
                s.AttachmentFileName,
                s.Status
            })
            .ToListAsync(cancellationToken);

        var submissionByStudent = submissions.ToDictionary(s => s.StudentId);

        var needsFileUrls = submissions.Any(s => s.AttachmentKey != null) || assignment.AttachmentKey != null;
        var fileStorage = needsFileUrls ? _serviceProvider.GetRequiredService<IFileStorageService>() : null;

        var rows = roster
            .OrderBy(r => r.Name)
            .Select(r =>
            {
                if (submissionByStudent.TryGetValue(r.Id, out var sub))
                {
                    return new AssignmentRosterRowDto
                    {
                        StudentId = r.Id,
                        StudentName = r.Name,
                        StudentNumber = r.StudentNumber,
                        SubmissionId = sub.Id,
                        Status = sub.Status,
                        SubmittedAt = sub.SubmittedAt,
                        Comment = sub.Comment,
                        Feedback = sub.Feedback,
                        Grade = sub.Grade,
                        AttachmentFileName = sub.AttachmentFileName,
                        AttachmentUrl = sub.AttachmentKey != null ? fileStorage!.GetFileUrl(sub.AttachmentKey) : null
                    };
                }

                return new AssignmentRosterRowDto
                {
                    StudentId = r.Id,
                    StudentName = r.Name,
                    StudentNumber = r.StudentNumber,
                    Status = "NotSubmitted"
                };
            })
            .ToList();

        var submittedCount = rows.Count(r => r.SubmissionId != null);
        var gradedCount = rows.Count(r => r.Status == SubmissionStatuses.Graded);

        var dto = new AssignmentRosterDto
        {
            Id = assignment.Id,
            Title = assignment.Title,
            Description = assignment.Description,
            ClassName = assignment.Class.Name,
            SubjectName = assignment.Subject.Name,
            DueDate = assignment.DueDate,
            AttachmentFileName = assignment.AttachmentFileName,
            AttachmentUrl = assignment.AttachmentKey != null ? fileStorage!.GetFileUrl(assignment.AttachmentKey) : null,
            RosterCount = rows.Count,
            SubmittedCount = submittedCount,
            GradedCount = gradedCount,
            MissingCount = rows.Count - submittedCount,
            Rows = rows
        };

        return Result<AssignmentRosterDto>.Success(dto);
    }
}
