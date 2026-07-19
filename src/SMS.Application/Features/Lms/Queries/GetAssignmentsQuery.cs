using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Lms.Queries;

public record GetAssignmentsQuery : IRequest<Result<List<AssignmentDto>>>
{
    public Guid? ClassId { get; init; }
    public Guid? SubjectId { get; init; }
    public Guid? AcademicTermId { get; init; }
}

public record AssignmentDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string ClassName { get; init; } = string.Empty;
    public string SubjectName { get; init; } = string.Empty;
    public DateTime DueDate { get; init; }
    public string? AttachmentFileName { get; init; }
    public bool IsPublished { get; init; }

    /// <summary>Students who have submitted anything (Submitted, Late or Graded).</summary>
    public int SubmissionCount { get; init; }
    /// <summary>Of those, how many have been graded.</summary>
    public int GradedCount { get; init; }
    /// <summary>Active students currently placed in the assignment's class - the denominator.</summary>
    public int RosterCount { get; init; }
}
