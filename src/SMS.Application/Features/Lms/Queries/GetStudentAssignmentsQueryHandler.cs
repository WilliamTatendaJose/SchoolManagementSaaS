using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Lms.Queries;

public class GetStudentAssignmentsQueryHandler : IRequestHandler<GetStudentAssignmentsQuery, Result<List<StudentAssignmentDto>>>
{
    private readonly IApplicationDbContext _context;
    // Resolved lazily (not constructor-injected): building a live IFileStorageService
    // constructs the AWS S3 client, which eagerly resolves AWS credentials and can hang
    // for 15-30s before failing wherever they aren't configured. Most assignments have
    // no attachment, so that cost/failure must not be paid on every list fetch.
    private readonly IServiceProvider _serviceProvider;

    public GetStudentAssignmentsQueryHandler(IApplicationDbContext context, IServiceProvider serviceProvider)
    {
        _context = context;
        _serviceProvider = serviceProvider;
    }

    public async Task<Result<List<StudentAssignmentDto>>> Handle(GetStudentAssignmentsQuery request, CancellationToken cancellationToken)
    {
        var student = await _context.Students.FirstOrDefaultAsync(s => s.Id == request.StudentId, cancellationToken);
        if (student == null)
        {
            return Result<List<StudentAssignmentDto>>.Failure("Student not found");
        }

        if (student.CurrentClassId == null)
        {
            return Result<List<StudentAssignmentDto>>.Success([]);
        }

        var rows = await _context.Assignments
            .AsNoTracking()
            .Include(a => a.Subject)
            .Where(a => a.ClassId == student.CurrentClassId && a.IsPublished)
            .OrderByDescending(a => a.DueDate)
            .Select(a => new
            {
                a.Id,
                a.Title,
                SubjectName = a.Subject.Name,
                a.DueDate,
                a.AttachmentKey,
                a.AttachmentFileName,
                Submission = a.Submissions
                    .Where(s => s.StudentId == request.StudentId)
                    .Select(s => new { s.Status, s.Grade })
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var fileStorage = rows.Any(a => a.AttachmentKey != null)
            ? _serviceProvider.GetRequiredService<IFileStorageService>()
            : null;

        var result = rows
            .Select(a => new StudentAssignmentDto
            {
                AssignmentId = a.Id,
                Title = a.Title,
                SubjectName = a.SubjectName,
                DueDate = a.DueDate,
                AttachmentFileName = a.AttachmentFileName,
                AttachmentUrl = a.AttachmentKey != null ? fileStorage!.GetFileUrl(a.AttachmentKey) : null,
                HasSubmitted = a.Submission != null,
                SubmissionStatus = a.Submission?.Status,
                Grade = a.Submission?.Grade
            })
            .ToList();

        return Result<List<StudentAssignmentDto>>.Success(result);
    }
}
