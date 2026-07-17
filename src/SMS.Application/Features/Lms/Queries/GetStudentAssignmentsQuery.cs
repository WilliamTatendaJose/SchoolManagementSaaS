using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Lms.Queries;

/// <summary>
/// Lists assignments set for a student's current class, together with the student's own
/// submission status (if any), most recently due first.
/// </summary>
public record GetStudentAssignmentsQuery(Guid StudentId) : IRequest<Result<List<StudentAssignmentDto>>>;

public record StudentAssignmentDto
{
    public Guid AssignmentId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string SubjectName { get; init; } = string.Empty;
    public DateTime DueDate { get; init; }
    public string? AttachmentFileName { get; init; }
    public string? AttachmentUrl { get; init; }
    public bool HasSubmitted { get; init; }
    public string? SubmissionStatus { get; init; }
    public decimal? Grade { get; init; }
}
