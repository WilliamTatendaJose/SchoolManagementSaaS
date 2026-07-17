using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Lms.Queries;

/// <summary>Lists all submissions for an assignment.</summary>
public record GetAssignmentSubmissionsQuery(Guid AssignmentId) : IRequest<Result<List<AssignmentSubmissionDto>>>;

public record AssignmentSubmissionDto
{
    public Guid Id { get; init; }
    public Guid StudentId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public DateTime SubmittedAt { get; init; }
    public string? Comment { get; init; }
    public string? AttachmentFileName { get; init; }
    public string? AttachmentUrl { get; init; }
    public decimal? Grade { get; init; }
    public string? Feedback { get; init; }
    public string Status { get; init; } = string.Empty;
}
