using SMS.Domain.Common;

namespace SMS.Domain.Entities;

/// <summary>
/// Many-to-many relationship between users and roles
/// </summary>
public class UserRole : TenantEntity
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Role Role { get; set; } = null!;
}
