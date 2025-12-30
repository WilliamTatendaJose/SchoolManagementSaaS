using SMS.Domain.Common;

namespace SMS.Domain.Entities;

/// <summary>
/// Many-to-many relationship between classes and subjects
/// </summary>
public class ClassSubject : TenantEntity
{
    public Guid ClassId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid? TeacherId { get; set; }
    public int PeriodsPerWeek { get; set; }
    
    // Navigation properties
    public virtual Class Class { get; set; } = null!;
    public virtual Subject Subject { get; set; } = null!;
    public virtual Staff? Teacher { get; set; }
}
