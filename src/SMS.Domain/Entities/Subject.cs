using SMS.Domain.Common;

namespace SMS.Domain.Entities;

/// <summary>
/// Represents a subject taught in the school
/// </summary>
public class Subject : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsCore { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual ICollection<ClassSubject> Classes { get; set; } = [];
    public virtual ICollection<TeacherSubject> Teachers { get; set; } = [];
    public virtual ICollection<Assessment> Assessments { get; set; } = [];
}
