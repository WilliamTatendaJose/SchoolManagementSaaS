using SMS.Domain.Common;

namespace SMS.Domain.Entities;

/// <summary>
/// Many-to-many relationship between teachers and subjects
/// </summary>
public class TeacherSubject : TenantEntity
{
    public Guid StaffId { get; set; }
    public Guid SubjectId { get; set; }
    public bool IsPrimary { get; set; }
    
    // Navigation properties
    public virtual Staff Staff { get; set; } = null!;
    public virtual Subject Subject { get; set; } = null!;
}
