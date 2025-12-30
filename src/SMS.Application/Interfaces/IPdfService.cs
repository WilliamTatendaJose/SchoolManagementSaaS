namespace SMS.Application.Interfaces;

/// <summary>
/// Interface for PDF generation
/// </summary>
public interface IPdfService
{
    Task<byte[]> GenerateReportCardAsync(Guid studentId, Guid termId, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateReceiptAsync(Guid paymentId, CancellationToken cancellationToken = default);
}
