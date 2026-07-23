using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.ParentPortal.Commands;

/// <summary>
/// Submits (or replaces, until graded) one of the caller's own children's work for an
/// assignment. Ownership-scoped: the caller must be the student's guardian, and the
/// assignment must belong to the student's class.
/// </summary>
public record SubmitMyChildAssignmentCommand : IRequest<Result<Guid>>
{
    public Guid StudentId { get; init; }
    public Guid AssignmentId { get; init; }
    public string? Comment { get; init; }
    public string? AttachmentFileName { get; init; }
    public string? AttachmentContentType { get; init; }
    public byte[]? AttachmentContent { get; init; }
}
