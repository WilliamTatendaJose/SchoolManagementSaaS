using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Lms.Commands;

/// <summary>
/// Publishes a course material to a class for a subject: either an external link or an
/// uploaded file (exactly one of the two).
/// </summary>
public record CreateCourseMaterialCommand : IRequest<Result<Guid>>
{
    public Guid ClassId { get; init; }
    public Guid SubjectId { get; init; }
    public Guid? AcademicTermId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }

    /// <summary>External link. Provide this OR an attachment, not both.</summary>
    public string? Url { get; init; }

    public string? AttachmentFileName { get; init; }
    public string? AttachmentContentType { get; init; }
    public byte[]? AttachmentContent { get; init; }
}
