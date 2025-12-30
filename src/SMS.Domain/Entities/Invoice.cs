using SMS.Domain.Common;
using SMS.Domain.Enums;

namespace SMS.Domain.Entities;

/// <summary>
/// Represents an invoice for student fees
/// </summary>
public class Invoice : AggregateRoot
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid StudentId { get; set; }
    public Guid AcademicTermId { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Balance => TotalAmount - DiscountAmount - PaidAmount;
    public string? Notes { get; set; }
    public bool IsPaid => Balance <= 0;
    
    // Navigation properties
    public virtual Student Student { get; set; } = null!;
    public virtual AcademicTerm AcademicTerm { get; set; } = null!;
    public virtual ICollection<InvoiceItem> Items { get; set; } = [];
    public virtual ICollection<Payment> Payments { get; set; } = [];
}
