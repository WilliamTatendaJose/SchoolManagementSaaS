using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Lms.Queries;

/// <summary>Published course materials for a student's current class (portal-facing).</summary>
public record GetStudentCourseMaterialsQuery(Guid StudentId) : IRequest<Result<List<StudentCourseMaterialDto>>>;

public record StudentCourseMaterialDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string SubjectName { get; init; } = string.Empty;
    public string? TermName { get; init; }

    /// <summary>"Link" for an external URL, "File" for an uploaded attachment.</summary>
    public string Kind { get; init; } = "Link";

    /// <summary>Where to open/download the material: the external URL or the file's download URL.</summary>
    public string? Link { get; init; }
    public string? FileName { get; init; }

    public DateTime CreatedAt { get; init; }
}
