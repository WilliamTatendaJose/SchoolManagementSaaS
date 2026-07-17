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

    /// <summary>Currency the payment was tendered in.</summary>
    public string Currency { get; set; } = "USD";

    /// <summary>Rate converting the payment currency to the invoice currency (1 for same currency).</summary>
    public decimal ExchangeRate { get; set; } = 1m;

    /// <summary>Payment amount expressed in the invoice's currency (what credits the invoice).</summary>
    public decimal AmountInInvoiceCurrency => Amount * ExchangeRate;

    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime PaymentDate { get; set; }
    public string? TransactionReference { get; set; }
    public string? MobileMoneyNumber { get; set; }
    public string? BankName { get; set; }
    public string? Notes { get; set; }
    public Guid? ReceivedById { get; set; }

    // Online payment gateway (e.g. Paynow) correlation
    public string? GatewayPollUrl { get; set; }
    
    // Navigation properties
    public virtual Invoice Invoice { get; set; } = null!;
    public virtual Staff? ReceivedBy { get; set; }
}
