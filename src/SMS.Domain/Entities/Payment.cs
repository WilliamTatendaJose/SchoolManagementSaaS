using SMS.Domain.Common;
using SMS.Domain.Enums;

namespace SMS.Domain.Entities;

/// <summary>
/// Represents a payment made against an invoice
/// </summary>
public class Payment : TenantEntity
{
    public string ReceiptNumber { get; set; } = string.Empty;
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime PaymentDate { get; set; }
    public string? TransactionReference { get; set; }
    public string? MobileMoneyNumber { get; set; }
    public string? BankName { get; set; }
    public string? Notes { get; set; }
    public Guid? ReceivedById { get; set; }
    
    // Navigation properties
    public virtual Invoice Invoice { get; set; } = null!;
    public virtual Staff? ReceivedBy { get; set; }
}
