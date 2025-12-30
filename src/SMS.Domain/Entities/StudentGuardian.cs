using SMS.Domain.Common;

namespace SMS.Domain.Entities;

/// <summary>
/// Many-to-many relationship between students and guardians
/// </summary>
public class StudentGuardian : TenantEntity
{
    public Guid StudentId { get; set; }
    public Guid GuardianId { get; set; }
    public string Relationship { get; set; } = string.Empty;
    public bool IsPrimaryContact { get; set; }
    public bool IsEmergencyContact { get; set; }
    public bool CanPickup { get; set; } = true;
    
    // Navigation properties
    public virtual Student Student { get; set; } = null!;
    public virtual Guardian Guardian { get; set; } = null!;
}
