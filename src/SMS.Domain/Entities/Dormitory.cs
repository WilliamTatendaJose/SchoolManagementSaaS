using SMS.Domain.Common;

namespace SMS.Domain.Entities;

/// <summary>
/// Represents a dormitory for boarding students
/// </summary>
public class Dormitory : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string? Gender { get; set; }
    public Guid? WardenId { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual Staff? Warden { get; set; }
    public virtual ICollection<Student> Students { get; set; } = [];
}
