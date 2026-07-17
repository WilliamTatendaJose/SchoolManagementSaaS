using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SMS.Application.Interfaces;

namespace SMS.Infrastructure.Services.Reports;

/// <summary>
/// Renders a payment receipt to PDF with QuestPDF (Community license).
/// </summary>
public class ReceiptPdfGenerator : IReceiptGenerator
{
    static ReceiptPdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Generate(ReceiptModel model)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(col =>
                {
                    col.Item().Text(model.SchoolName).FontSize(16).SemiBold();
                    col.Item().Text("Payment Receipt").FontSize(12);
                    col.Item().PaddingTop(2).Text($"Receipt No: {model.ReceiptNumber}");
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Spacing(6);
                    col.Item().Text($"Date: {model.PaymentDate:dd MMM yyyy}");
                    col.Item().Text($"Student: {model.StudentName} ({model.StudentNumber})");
                    col.Item().Text($"Invoice: {model.InvoiceNumber}");
                    col.Item().Text($"Method: {model.PaymentMethod}");

                    col.Item().PaddingTop(6)
                        .Text($"Amount paid: {model.Currency} {model.Amount.ToString("0.00", CultureInfo.InvariantCulture)}")
                        .FontSize(13).SemiBold();

                    col.Item().Text($"Remaining balance: {model.Currency} {model.RemainingBalance.ToString("0.00", CultureInfo.InvariantCulture)}");
                });

                page.Footer().AlignCenter().Text("Thank you.").FontSize(9);
            });
        }).GeneratePdf();
    }
}
