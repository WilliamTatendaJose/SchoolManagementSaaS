using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;

namespace SMS.Application.Features.Lms.Commands;

public class CreateCourseMaterialCommandHandler : IRequestHandler<CreateCourseMaterialCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    // Resolved lazily (not constructor-injected) so storage is only touched for file
    // materials, not links. File storage is DB-backed (no external dependency).
    private readonly IServiceProvider _serviceProvider;

    public CreateCourseMaterialCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, IServiceProvider serviceProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _serviceProvider = serviceProvider;
    }

    public async Task<Result<Guid>> Handle(CreateCourseMaterialCommand request, CancellationToken cancellationToken)
    {
        if (!await _context.Classes.AnyAsync(c => c.Id == request.ClassId, cancellationToken))
        {
            return Result<Guid>.Failure("Class not found");
        }

        if (!await _context.Subjects.AnyAsync(s => s.Id == request.SubjectId, cancellationToken))
        {
            return Result<Guid>.Failure("Subject not found");
        }

        if (request.AcademicTermId is { } termId &&
            !await _context.AcademicTerms.AnyAsync(t => t.Id == termId, cancellationToken))
        {
            return Result<Guid>.Failure("Academic term not found");
        }

        var uploadedByStaffId = await _context.Staff
            .Where(s => s.UserId == _currentUser.UserId)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var material = new CourseMaterial
        {
            ClassId = request.ClassId,
            SubjectId = request.SubjectId,
            AcademicTermId = request.AcademicTermId,
            Title = request.Title.Trim(),
            Description = request.Description,
            Url = string.IsNullOrWhiteSpace(request.Url) ? null : request.Url.Trim(),
            UploadedByStaffId = uploadedByStaffId,
            IsPublished = true
        };

        if (request.AttachmentContent is { Length: > 0 } && request.AttachmentFileName != null)
        {
            var fileStorage = _serviceProvider.GetRequiredService<IFileStorageService>();
            using var stream = new MemoryStream(request.AttachmentContent);
            material.AttachmentKey = await fileStorage.UploadAsync(
                stream, request.AttachmentFileName, request.AttachmentContentType ?? "application/octet-stream", cancellationToken);
            material.AttachmentFileName = request.AttachmentFileName;
            material.ContentType = request.AttachmentContentType;
        }

        _context.CourseMaterials.Add(material);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(material.Id);
    }
}
