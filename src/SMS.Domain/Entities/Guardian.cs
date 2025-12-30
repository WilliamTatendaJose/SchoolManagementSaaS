using SMS.Domain.Common;
using SMS.Domain.Enums;

namespace SMS.Domain.Entities;

/// <summary>
/// Represents a parent or guardian of a student
/// </summary>
public class Guardian : TenantEntity
{
    public Guid? UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? NationalId { get; set; }
    public Gender Gender { get; set; }
    public string? Phone { get; set; }
    public string? AlternatePhone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Occupation { get; set; }
    public string? Employer { get; set; }
    
    public string FullName => $"{FirstName} {LastName}";
    
    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual User? User { get; set; }
    public virtual ICollection<StudentGuardian> Students { get; set; } = [];
}
