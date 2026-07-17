using SMS.Domain.Common;

namespace SMS.Domain.Entities;

/// <summary>
/// Persisted class-teacher and head-teacher remarks for a student's report card in a given
/// term, so they only need to be typed once and are reused across regenerations.
/// </summary>
public class ReportCardComment : TenantEntity
{
    public Guid StudentId { get; set; }
    public Guid AcademicTermId { get; set; }
    public string? ClassTeacherComment { get; set; }
    public string? HeadComment { get; set; }

    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual Student Student { get; set; } = null!;
    public virtual AcademicTerm AcademicTerm { get; set; } = null!;
}
