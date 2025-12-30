using SMS.Domain.Common;

namespace SMS.Domain.Entities;

/// <summary>
/// Represents an asset in the school
/// </summary>
public class Asset : TenantEntity
{
    public string AssetNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Location { get; set; }
    public decimal? PurchasePrice { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public string Condition { get; set; } = "Good";
    public Guid? AssignedToId { get; set; }
    public DateTime? AssignedDate { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual Staff? AssignedTo { get; set; }
}
