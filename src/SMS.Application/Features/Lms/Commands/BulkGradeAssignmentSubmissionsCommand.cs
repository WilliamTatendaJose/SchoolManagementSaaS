using MediatR;
using Result = SMS.Application.Common.Models.Result;

namespace SMS.Application.Features.Lms.Commands;

/// <summary>
/// Grades a whole class in one save (the mark-sheet workflow). Each entry grades an
/// existing submission, or - for a student who handed in on paper with no digital
/// submission - creates a graded submission on the spot. Entries with a null grade are
/// skipped, so a teacher can save a partially-filled sheet.
/// </summary>
public record BulkGradeAssignmentSubmissionsCommand : IRequest<Result>
{
    public Guid AssignmentId { get; init; }
    public List<AssignmentGradeEntry> Grades { get; init; } = [];
}

public record AssignmentGradeEntry
{
    public Guid StudentId { get; init; }
    public decimal? Grade { get; init; }
    public string? Feedback { get; init; }
}
