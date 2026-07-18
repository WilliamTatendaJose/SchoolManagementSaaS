using SMS.Domain.Common;

namespace SMS.Domain.Entities;

/// <summary>
/// A ledger entry against a student's prepaid account balance. Positive amounts are
/// credits (deposits, change issued as credit); negative amounts are debits (applied
/// to an invoice as a payment). The student's balance is the running sum.
/// </summary>
public class StudentAccountTransaction : TenantEntity
{
    public Guid StudentId { get; set; }
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
    public Guid? InvoiceId { get; set; }
    public Guid? PaymentId { get; set; }
    public string? Notes { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Student Student { get; set; } = null!;
    public virtual Invoice? Invoice { get; set; }
    public virtual Payment? Payment { get; set; }
}
