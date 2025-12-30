using SMS.Domain.Common;

namespace SMS.Domain.Entities;

/// <summary>
/// Represents a student enrollment in a class for a specific academic year/term
/// </summary>
public class Enrollment : TenantEntity
{
    public Guid StudentId { get; set; }
    public Guid ClassId { get; set; }
    public Guid? StreamId { get; set; }
    public Guid AcademicYearId { get; set; }
    public DateTime EnrollmentDate { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public virtual Student Student { get; set; } = null!;
    public virtual Class Class { get; set; } = null!;
    public virtual Stream? Stream { get; set; }
    public virtual AcademicYear AcademicYear { get; set; } = null!;
}
