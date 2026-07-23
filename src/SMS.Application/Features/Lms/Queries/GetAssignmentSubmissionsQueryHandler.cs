using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Lms.Queries;

public class GetAssignmentSubmissionsQueryHandler : IRequestHandler<GetAssignmentSubmissionsQuery, Result<List<AssignmentSubmissionDto>>>
{
    private readonly IApplicationDbContext _context;
    // Resolved lazily (not constructor-injected) so storage is only touched when a file
    // download URL is actually needed. File storage is DB-backed (no external dependency).
    private readonly IServiceProvider _serviceProvider;

    public GetAssignmentSubmissionsQueryHandler(IApplicationDbContext context, IServiceProvider serviceProvider)
    {
        _context = context;
        _serviceProvider = serviceProvider;
    }

    public async Task<Result<List<AssignmentSubmissionDto>>> Handle(GetAssignmentSubmissionsQuery request, CancellationToken cancellationToken)
    {
        if (!await _context.Assignments.AnyAsync(a => a.Id == request.AssignmentId, cancellationToken))
        {
            return Result<List<AssignmentSubmissionDto>>.Failure("Assignment not found");
        }

        var submissions = await _context.AssignmentSubmissions
            .AsNoTracking()
            .Include(s => s.Student)
            .Where(s => s.AssignmentId == request.AssignmentId)
            .OrderBy(s => s.Student.LastName).ThenBy(s => s.Student.FirstName)
            .Select(s => new
            {
                s.Id,
                s.StudentId,
                StudentName = s.Student.FirstName + " " + s.Student.LastName,
                s.SubmittedAt,
                s.Comment,
                s.AttachmentKey,
                s.AttachmentFileName,
                s.Grade,
                s.Feedback,
                s.Status
            })
            .ToListAsync(cancellationToken);

        var fileStorage = submissions.Any(s => s.AttachmentKey != null)
            ? _serviceProvider.GetRequiredService<IFileStorageService>()
            : null;

        var result = submissions
            .Select(s => new AssignmentSubmissionDto
            {
                Id = s.Id,
                StudentId = s.StudentId,
                StudentName = s.StudentName,
                SubmittedAt = s.SubmittedAt,
                Comment = s.Comment,
                AttachmentFileName = s.AttachmentFileName,
                AttachmentUrl = s.AttachmentKey != null ? fileStorage!.GetFileUrl(s.AttachmentKey) : null,
                Grade = s.Grade,
                Feedback = s.Feedback,
                Status = s.Status
            })
            .ToList();

        return Result<List<AssignmentSubmissionDto>>.Success(result);
    }
}
