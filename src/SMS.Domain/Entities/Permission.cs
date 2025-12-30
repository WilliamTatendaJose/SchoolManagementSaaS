using SMS.Domain.Common;

namespace SMS.Domain.Entities;

/// <summary>
/// Represents a permission that can be assigned to a role
/// </summary>
public class Permission : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Module { get; set; } = string.Empty;
    
    // Navigation properties
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = [];
}
