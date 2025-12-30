using SMS.Domain.Common;

namespace SMS.Domain.Entities;

/// <summary>
/// Represents an academic term within a year
/// </summary>
public class AcademicTerm : TenantEntity
{
    public Guid AcademicYearId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TermNumber { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsCurrent { get; set; }
    
    // Navigation properties
    public virtual AcademicYear AcademicYear { get; set; } = null!;
    public virtual ICollection<Assessment> Assessments { get; set; } = [];
    public virtual ICollection<Invoice> Invoices { get; set; } = [];
}
