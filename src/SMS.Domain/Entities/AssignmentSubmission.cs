using SMS.Domain.Common;

namespace SMS.Domain.Entities;

/// <summary>
/// A student's submission for an <see cref="Assignment"/>. A student has at most one
/// submission per assignment; resubmitting before it is graded replaces the attachment
/// and comment in place.
/// </summary>
public class AssignmentSubmission : TenantEntity
{
    public Guid AssignmentId { get; set; }
    public Guid StudentId { get; set; }
    public DateTime SubmittedAt { get; set; }
    public string? Comment { get; set; }
    public string? AttachmentKey { get; set; }
    public string? AttachmentFileName { get; set; }
    public decimal? Grade { get; set; }
    public string? Feedback { get; set; }

    /// <summary>Submitted, Late or Graded.</summary>
    public string Status { get; set; } = "Submitted";

    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual Assignment Assignment { get; set; } = null!;
    public virtual Student Student { get; set; } = null!;
}
