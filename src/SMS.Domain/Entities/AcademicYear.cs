using SMS.Domain.Common;

namespace SMS.Domain.Entities;

/// <summary>
/// Represents an academic year
/// </summary>
public class AcademicYear : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public int Year { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsCurrent { get; set; }
    
    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual ICollection<AcademicTerm> Terms { get; set; } = [];
}
