using SMS.Domain.Common;

namespace SMS.Domain.Entities;

/// <summary>
/// Represents a role that can be assigned to users
/// </summary>
public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    
    // Navigation properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = [];
    public virtual ICollection<RolePermission> Permissions { get; set; } = [];
}
