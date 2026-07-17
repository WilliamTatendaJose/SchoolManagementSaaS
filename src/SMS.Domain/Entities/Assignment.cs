using SMS.Domain.Common;

namespace SMS.Domain.Entities;

/// <summary>
/// A homework/assignment set for a class and subject, with an optional attachment
/// (stored via <c>IFileStorageService</c>) that students can download.
/// </summary>
public class Assignment : TenantEntity
{
    public Guid ClassId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid AcademicTermId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DueDate { get; set; }
    public string? AttachmentKey { get; set; }
    public string? AttachmentFileName { get; set; }
    public bool IsPublished { get; set; } = true;

    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual Class Class { get; set; } = null!;
    public virtual Subject Subject { get; set; } = null!;
    public virtual AcademicTerm AcademicTerm { get; set; } = null!;
    public virtual ICollection<AssignmentSubmission> Submissions { get; set; } = [];
}
