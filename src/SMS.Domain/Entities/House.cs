using SMS.Domain.Common;

namespace SMS.Domain.Entities;

/// <summary>
/// Represents a house/team for inter-house activities
/// </summary>
public class House : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public Guid? HouseMasterId { get; set; }
    
    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual Staff? HouseMaster { get; set; }
    public virtual ICollection<Student> Students { get; set; } = [];
}
