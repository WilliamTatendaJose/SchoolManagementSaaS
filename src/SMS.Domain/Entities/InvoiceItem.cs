using SMS.Domain.Common;

namespace SMS.Domain.Entities;

/// <summary>
/// Represents a line item on an invoice
/// </summary>
public class InvoiceItem : TenantEntity
{
    public Guid InvoiceId { get; set; }
    public Guid? FeeStructureId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal TotalAmount => Amount * Quantity;
    
    // Navigation properties
    public virtual Invoice Invoice { get; set; } = null!;
    public virtual FeeStructure? FeeStructure { get; set; }
}
