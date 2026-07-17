namespace SMS.Application.Interfaces;

/// <summary>
/// Renders a payment receipt model to a PDF document. Implemented in Infrastructure.
/// </summary>
public interface IReceiptGenerator
{
    byte[] Generate(ReceiptModel model);
}

public record ReceiptModel
{
    public string SchoolName { get; init; } = string.Empty;
    public string ReceiptNumber { get; init; } = string.Empty;
    public string StudentName { get; init; } = string.Empty;
    public string StudentNumber { get; init; } = string.Empty;
    public string InvoiceNumber { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public string PaymentMethod { get; init; } = string.Empty;
    public DateTime PaymentDate { get; init; }
    public decimal RemainingBalance { get; init; }
}
