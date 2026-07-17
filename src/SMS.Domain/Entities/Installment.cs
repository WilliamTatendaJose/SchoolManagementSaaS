using SMS.Domain.Common;

namespace SMS.Domain.Entities;

/// <summary>
/// A single scheduled installment within a payment plan.
/// </summary>
public class Installment : TenantEntity
{
    public Guid PaymentPlanId { get; set; }
    public int SequenceNumber { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidDate { get; set; }

    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual PaymentPlan PaymentPlan { get; set; } = null!;
}
