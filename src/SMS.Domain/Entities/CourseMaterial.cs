using SMS.Domain.Common;

namespace SMS.Domain.Entities;

/// <summary>
/// A piece of course content shared with a class for a subject: either an uploaded file
/// (stored via <c>IFileStorageService</c>) or an external link (e.g. a video, a shared
/// drive). Students and their guardians can view/download it through the portal.
/// </summary>
public class CourseMaterial : TenantEntity
{
    public Guid ClassId { get; set; }
    public Guid SubjectId { get; set; }
    /// <summary>Optional: pin the material to a specific term, or leave null for general.</summary>
    public Guid? AcademicTermId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>External link (for a "link" material). Null for an uploaded file.</summary>
    public string? Url { get; set; }

    /// <summary>Storage key for an uploaded file. Null for a link.</summary>
    public string? AttachmentKey { get; set; }
    public string? AttachmentFileName { get; set; }
    public string? ContentType { get; set; }

    public Guid? UploadedByStaffId { get; set; }
    public bool IsPublished { get; set; } = true;

    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual Class Class { get; set; } = null!;
    public virtual Subject Subject { get; set; } = null!;
    public virtual AcademicTerm? AcademicTerm { get; set; }
    public virtual Staff? UploadedByStaff { get; set; }
}
