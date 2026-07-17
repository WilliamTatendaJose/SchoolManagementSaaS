using SMS.Application.Interfaces;
using SMS.Domain.Entities;
using SMS.Domain.Enums;

namespace SMS.Application.Features.Payments;

/// <summary>
/// Applies a gateway status result to a payment/invoice pair. Idempotent: a payment that
/// is already completed is never settled twice, so repeated callbacks or polls cannot
/// double-credit an invoice.
/// </summary>
internal static class PaymentSettlement
{
    /// <summary>
    /// Returns true when the payment transitioned to Completed and the invoice was credited.
    /// </summary>
    public static bool Apply(Payment payment, Invoice invoice, PaymentStatusResult status)
    {
        if (payment.Status == PaymentStatus.Completed)
        {
            return false;
        }

        if (status.IsPaid)
        {
            payment.Status = PaymentStatus.Completed;
            payment.PaymentDate = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(status.GatewayReference))
            {
                payment.Notes = $"Gateway reference: {status.GatewayReference}";
            }

            invoice.PaidAmount += payment.AmountInInvoiceCurrency;
            return true;
        }

        payment.Status = status.Status switch
        {
            GatewayPaymentStatus.Cancelled => PaymentStatus.Cancelled,
            GatewayPaymentStatus.Refunded => PaymentStatus.Refunded,
            GatewayPaymentStatus.Failed => PaymentStatus.Failed,
            _ => PaymentStatus.Pending
        };

        return false;
    }
}
