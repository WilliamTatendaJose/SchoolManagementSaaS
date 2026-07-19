using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Lms.Queries;

/// <summary>
/// Returns everything the assignment detail page needs in one call: the assignment
/// header, a progress summary, and one row per student currently in the class -
/// including those who have <em>not</em> submitted, which is the teacher's real question.
/// </summary>
public record GetAssignmentRosterQuery(Guid AssignmentId) : IRequest<Result<AssignmentRosterDto>>;

public record AssignmentRosterDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string ClassName { get; init; } = string.Empty;
    public string SubjectName { get; init; } = string.Empty;
    public DateTime DueDate { get; init; }
    public string? AttachmentFileName { get; init; }
    public string? AttachmentUrl { get; init; }

    public int RosterCount { get; init; }
    public int SubmittedCount { get; init; }
    public int GradedCount { get; init; }
    public int MissingCount { get; init; }

    public List<AssignmentRosterRowDto> Rows { get; init; } = [];
}

public record AssignmentRosterRowDto
{
    public Guid StudentId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public string StudentNumber { get; init; } = string.Empty;

    /// <summary>Null when the student has no submission yet.</summary>
    public Guid? SubmissionId { get; init; }
    /// <summary>NotSubmitted, Submitted, Late or Graded.</summary>
    public string Status { get; init; } = string.Empty;
    public DateTime? SubmittedAt { get; init; }
    public string? Comment { get; init; }
    public string? Feedback { get; init; }
    public decimal? Grade { get; init; }
    public string? AttachmentFileName { get; init; }
    public string? AttachmentUrl { get; init; }
}
