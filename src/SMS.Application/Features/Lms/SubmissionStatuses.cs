namespace SMS.Application.Features.Lms;

/// <summary>Lifecycle statuses for an <c>AssignmentSubmission</c> (stored on its <c>Status</c>).</summary>
public static class SubmissionStatuses
{
    public const string Submitted = "Submitted";
    public const string Late = "Late";
    public const string Graded = "Graded";
}
