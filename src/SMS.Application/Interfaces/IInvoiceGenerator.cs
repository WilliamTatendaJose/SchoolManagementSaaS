using SMS.Application.Common.Branding;

namespace SMS.Application.Interfaces;

/// <summary>
/// Renders an invoice model to a PDF document. Implemented in Infrastructure.
/// </summary>
public interface IInvoiceGenerator
{
    byte[] Generate(InvoiceModel model);
}

public record InvoiceModel
{
    public SchoolBranding Branding { get; init; } = new();
    public string InvoiceNumber { get; init; } = string.Empty;
    public string StudentName { get; init; } = string.Empty;
    public string StudentNumber { get; init; } = string.Empty;
    public string ClassName { get; init; } = string.Empty;
    public string TermName { get; init; } = string.Empty;
    public string Currency { get; init; } = "USD";
    public DateTime InvoiceDate { get; init; }
    public DateTime DueDate { get; init; }
    public List<InvoiceLine> Lines { get; init; } = [];
    public decimal Subtotal { get; init; }
    public decimal Discount { get; init; }
    public decimal Paid { get; init; }
    public decimal Balance { get; init; }
    public string? Notes { get; init; }
}

public record InvoiceLine
{
    public string Description { get; init; } = string.Empty;
    public int Quantity { get; init; } = 1;
    public decimal Amount { get; init; }
    public decimal LineTotal { get; init; }
}
