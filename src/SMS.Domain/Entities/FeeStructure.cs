using SMS.Domain.Common;

namespace SMS.Domain.Entities;

/// <summary>
/// Represents a fee structure for a class
/// </summary>
public class FeeStructure : TenantEntity
{
    public Guid ClassId { get; set; }
    public Guid AcademicYearId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public string FeeType { get; set; } = string.Empty;
    public bool IsRecurring { get; set; }
    public bool IsOptional { get; set; }
    
    // Navigation properties
    public virtual Class Class { get; set; } = null!;
    public virtual AcademicYear AcademicYear { get; set; } = null!;
}
