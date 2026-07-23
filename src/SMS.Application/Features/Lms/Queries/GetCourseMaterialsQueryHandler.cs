using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Lms.Queries;

public class GetCourseMaterialsQueryHandler : IRequestHandler<GetCourseMaterialsQuery, Result<List<CourseMaterialDto>>>
{
    private readonly IApplicationDbContext _context;
    // Resolved lazily (not constructor-injected) so storage is only touched when there's a
    // file material to build a download URL for. File storage is DB-backed (no external dependency).
    private readonly IServiceProvider _serviceProvider;

    public GetCourseMaterialsQueryHandler(IApplicationDbContext context, IServiceProvider serviceProvider)
    {
        _context = context;
        _serviceProvider = serviceProvider;
    }

    public async Task<Result<List<CourseMaterialDto>>> Handle(GetCourseMaterialsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.CourseMaterials
            .AsNoTracking()
            .Include(m => m.Class)
            .Include(m => m.Subject)
            .Include(m => m.AcademicTerm)
            .Include(m => m.UploadedByStaff!).ThenInclude(s => s.User)
            .AsQueryable();

        if (request.ClassId.HasValue)
        {
            query = query.Where(m => m.ClassId == request.ClassId.Value);
        }

        if (request.SubjectId.HasValue)
        {
            query = query.Where(m => m.SubjectId == request.SubjectId.Value);
        }

        if (request.AcademicTermId.HasValue)
        {
            query = query.Where(m => m.AcademicTermId == request.AcademicTermId.Value);
        }

        var rows = await query
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new
            {
                m.Id,
                m.Title,
                m.Description,
                ClassName = m.Class.Name,
                SubjectName = m.Subject.Name,
                TermName = m.AcademicTerm != null ? m.AcademicTerm.Name : null,
                m.Url,
                m.AttachmentKey,
                m.AttachmentFileName,
                UploadedByName = m.UploadedByStaff != null ? m.UploadedByStaff.User.FirstName + " " + m.UploadedByStaff.User.LastName : null,
                m.CreatedAt,
                m.IsPublished
            })
            .ToListAsync(cancellationToken);

        var fileStorage = rows.Any(m => m.AttachmentKey != null)
            ? _serviceProvider.GetRequiredService<IFileStorageService>()
            : null;

        var result = rows
            .Select(m => new CourseMaterialDto
            {
                Id = m.Id,
                Title = m.Title,
                Description = m.Description,
                ClassName = m.ClassName,
                SubjectName = m.SubjectName,
                TermName = m.TermName,
                Kind = m.AttachmentKey != null ? "File" : "Link",
                Url = m.Url,
                AttachmentFileName = m.AttachmentFileName,
                DownloadUrl = m.AttachmentKey != null ? fileStorage!.GetFileUrl(m.AttachmentKey) : null,
                UploadedByName = string.IsNullOrWhiteSpace(m.UploadedByName) ? null : m.UploadedByName,
                CreatedAt = m.CreatedAt,
                IsPublished = m.IsPublished
            })
            .ToList();

        return Result<List<CourseMaterialDto>>.Success(result);
    }
}
