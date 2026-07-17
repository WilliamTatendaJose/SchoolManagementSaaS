using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Lms.Commands;

/// <summary>
/// Records (or, if the student already has an ungraded submission for this assignment,
/// replaces) a student's submission.
/// </summary>
public record RecordAssignmentSubmissionCommand : IRequest<Result<Guid>>
{
    public Guid AssignmentId { get; init; }
    public Guid StudentId { get; init; }
    public string? Comment { get; init; }
    public DateTime? SubmittedAt { get; init; }
    public string? AttachmentFileName { get; init; }
    public string? AttachmentContentType { get; init; }
    public byte[]? AttachmentContent { get; init; }
}
