using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Lms.Queries;

public class GetStudentCourseMaterialsQueryHandler : IRequestHandler<GetStudentCourseMaterialsQuery, Result<List<StudentCourseMaterialDto>>>
{
    private readonly IApplicationDbContext _context;
    // Resolved lazily (not constructor-injected) so storage is only touched when there's a
    // file material to build a download URL for. File storage is DB-backed (no external dependency).
    private readonly IServiceProvider _serviceProvider;

    public GetStudentCourseMaterialsQueryHandler(IApplicationDbContext context, IServiceProvider serviceProvider)
    {
        _context = context;
        _serviceProvider = serviceProvider;
    }

    public async Task<Result<List<StudentCourseMaterialDto>>> Handle(GetStudentCourseMaterialsQuery request, CancellationToken cancellationToken)
    {
        var student = await _context.Students.FirstOrDefaultAsync(s => s.Id == request.StudentId, cancellationToken);
        if (student == null)
        {
            return Result<List<StudentCourseMaterialDto>>.Failure("Student not found");
        }

        if (student.CurrentClassId == null)
        {
            return Result<List<StudentCourseMaterialDto>>.Success([]);
        }

        var rows = await _context.CourseMaterials
            .AsNoTracking()
            .Include(m => m.Subject)
            .Include(m => m.AcademicTerm)
            .Where(m => m.ClassId == student.CurrentClassId && m.IsPublished)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new
            {
                m.Id,
                m.Title,
                m.Description,
                SubjectName = m.Subject.Name,
                TermName = m.AcademicTerm != null ? m.AcademicTerm.Name : null,
                m.Url,
                m.AttachmentKey,
                m.AttachmentFileName,
                m.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var fileStorage = rows.Any(m => m.AttachmentKey != null)
            ? _serviceProvider.GetRequiredService<IFileStorageService>()
            : null;

        var result = rows
            .Select(m => new StudentCourseMaterialDto
            {
                Id = m.Id,
                Title = m.Title,
                Description = m.Description,
                SubjectName = m.SubjectName,
                TermName = m.TermName,
                Kind = m.AttachmentKey != null ? "File" : "Link",
                Link = m.AttachmentKey != null ? fileStorage!.GetFileUrl(m.AttachmentKey) : m.Url,
                FileName = m.AttachmentFileName,
                CreatedAt = m.CreatedAt
            })
            .ToList();

        return Result<List<StudentCourseMaterialDto>>.Success(result);
    }
}
