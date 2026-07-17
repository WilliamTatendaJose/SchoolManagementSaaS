using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Lms.Commands;

public record CreateAssignmentCommand : IRequest<Result<Guid>>
{
    public Guid ClassId { get; init; }
    public Guid SubjectId { get; init; }
    public Guid AcademicTermId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime DueDate { get; init; }

    /// <summary>Optional attachment; all three fields must be set together or all omitted.</summary>
    public string? AttachmentFileName { get; init; }
    public string? AttachmentContentType { get; init; }
    public byte[]? AttachmentContent { get; init; }
}
