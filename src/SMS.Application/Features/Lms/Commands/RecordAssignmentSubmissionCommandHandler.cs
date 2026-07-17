using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;

namespace SMS.Application.Features.Lms.Commands;

public class RecordAssignmentSubmissionCommandHandler : IRequestHandler<RecordAssignmentSubmissionCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IFileStorageService _fileStorage;

    public RecordAssignmentSubmissionCommandHandler(IApplicationDbContext context, IFileStorageService fileStorage)
    {
        _context = context;
        _fileStorage = fileStorage;
    }

    public async Task<Result<Guid>> Handle(RecordAssignmentSubmissionCommand request, CancellationToken cancellationToken)
    {
        var assignment = await _context.Assignments.FirstOrDefaultAsync(a => a.Id == request.AssignmentId, cancellationToken);
        if (assignment == null)
        {
            return Result<Guid>.Failure("Assignment not found");
        }

        if (!await _context.Students.AnyAsync(s => s.Id == request.StudentId, cancellationToken))
        {
            return Result<Guid>.Failure("Student not found");
        }

        var submittedAt = request.SubmittedAt ?? DateTime.UtcNow;
        var status = submittedAt > assignment.DueDate ? SubmissionStatuses.Late : SubmissionStatuses.Submitted;

        var submission = await _context.AssignmentSubmissions
            .FirstOrDefaultAsync(s => s.AssignmentId == request.AssignmentId && s.StudentId == request.StudentId, cancellationToken);

        if (submission != null && submission.Status == SubmissionStatuses.Graded)
        {
            return Result<Guid>.Failure("This submission has already been graded and can no longer be replaced");
        }

        if (submission == null)
        {
            submission = new AssignmentSubmission
            {
                AssignmentId = request.AssignmentId,
                StudentId = request.StudentId
            };
            _context.AssignmentSubmissions.Add(submission);
        }

        submission.SubmittedAt = submittedAt;
        submission.Comment = request.Comment;
        submission.Status = status;

        if (request.AttachmentContent is { Length: > 0 } && request.AttachmentFileName != null)
        {
            using var stream = new MemoryStream(request.AttachmentContent);
            submission.AttachmentKey = await _fileStorage.UploadAsync(
                stream, request.AttachmentFileName, request.AttachmentContentType ?? "application/octet-stream", cancellationToken);
            submission.AttachmentFileName = request.AttachmentFileName;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(submission.Id);
    }
}
