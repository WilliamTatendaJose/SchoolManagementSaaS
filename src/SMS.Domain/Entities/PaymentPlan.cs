using SMS.Domain.Common;

namespace SMS.Domain.Entities;

/// <summary>
/// An agreement to settle an invoice in scheduled installments.
/// </summary>
public class PaymentPlan : TenantEntity
{
    public Guid InvoiceId { get; set; }
    public int InstallmentCount { get; set; }
    public DateTime StartDate { get; set; }
    public string Status { get; set; } = "Active";

    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual Invoice Invoice { get; set; } = null!;
    public virtual ICollection<Installment> Installments { get; set; } = [];
}
