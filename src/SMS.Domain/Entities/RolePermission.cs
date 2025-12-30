using SMS.Domain.Common;

namespace SMS.Domain.Entities;

/// <summary>
/// Many-to-many relationship between roles and permissions
/// </summary>
public class RolePermission : BaseEntity
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
    
    // Navigation properties
    public virtual Role Role { get; set; } = null!;
    public virtual Permission Permission { get; set; } = null!;
}
