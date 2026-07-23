using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Lms.Queries;

public record GetCourseMaterialsQuery : IRequest<Result<List<CourseMaterialDto>>>
{
    public Guid? ClassId { get; init; }
    public Guid? SubjectId { get; init; }
    public Guid? AcademicTermId { get; init; }
}

public record CourseMaterialDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string ClassName { get; init; } = string.Empty;
    public string SubjectName { get; init; } = string.Empty;
    public string? TermName { get; init; }

    /// <summary>"Link" for an external URL, "File" for an uploaded attachment.</summary>
    public string Kind { get; init; } = "Link";

    /// <summary>External link (when Kind == "Link").</summary>
    public string? Url { get; init; }

    /// <summary>Original file name (when Kind == "File").</summary>
    public string? AttachmentFileName { get; init; }
    /// <summary>Download URL for the uploaded file (when Kind == "File").</summary>
    public string? DownloadUrl { get; init; }

    public string? UploadedByName { get; init; }
    public DateTime CreatedAt { get; init; }
    public bool IsPublished { get; init; }
}
