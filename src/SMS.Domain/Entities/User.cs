using SMS.Domain.Common;
using SMS.Domain.Enums;

namespace SMS.Domain.Entities;

/// <summary>
/// Represents a user in the system with role-based access
/// </summary>
public class User : TenantEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? ProfilePicture { get; set; }
    public bool IsActive { get; set; } = true;
    public bool EmailConfirmed { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    
    public string FullName => $"{FirstName} {LastName}";
    
    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual ICollection<UserRole> Roles { get; set; } = [];
    public virtual Staff? Staff { get; set; }
    public virtual Guardian? Guardian { get; set; }
}
