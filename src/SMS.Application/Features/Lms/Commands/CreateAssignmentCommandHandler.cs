using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;

namespace SMS.Application.Features.Lms.Commands;

public class CreateAssignmentCommandHandler : IRequestHandler<CreateAssignmentCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    // Resolved lazily (not constructor-injected): building a live IFileStorageService
    // constructs the AWS S3 client, which eagerly resolves AWS credentials and can hang
    // for 15-30s before failing wherever they aren't configured. Most assignments have
    // no attachment, so that cost/failure must not be paid on every create.
    private readonly IServiceProvider _serviceProvider;

    public CreateAssignmentCommandHandler(IApplicationDbContext context, IServiceProvider serviceProvider)
    {
        _context = context;
        _serviceProvider = serviceProvider;
    }

    public async Task<Result<Guid>> Handle(CreateAssignmentCommand request, CancellationToken cancellationToken)
    {
        if (!await _context.Classes.AnyAsync(c => c.Id == request.ClassId, cancellationToken))
        {
            return Result<Guid>.Failure("Class not found");
        }

        if (!await _context.Subjects.AnyAsync(s => s.Id == request.SubjectId, cancellationToken))
        {
            return Result<Guid>.Failure("Subject not found");
        }

        if (!await _context.AcademicTerms.AnyAsync(t => t.Id == request.AcademicTermId, cancellationToken))
        {
            return Result<Guid>.Failure("Academic term not found");
        }

        var assignment = new Assignment
        {
            ClassId = request.ClassId,
            SubjectId = request.SubjectId,
            AcademicTermId = request.AcademicTermId,
            Title = request.Title,
            Description = request.Description,
            DueDate = request.DueDate,
            IsPublished = true
        };

        if (request.AttachmentContent is { Length: > 0 } && request.AttachmentFileName != null)
        {
            var fileStorage = _serviceProvider.GetRequiredService<IFileStorageService>();
            using var stream = new MemoryStream(request.AttachmentContent);
            assignment.AttachmentKey = await fileStorage.UploadAsync(
                stream, request.AttachmentFileName, request.AttachmentContentType ?? "application/octet-stream", cancellationToken);
            assignment.AttachmentFileName = request.AttachmentFileName;
        }

        _context.Assignments.Add(assignment);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(assignment.Id);
    }
}
