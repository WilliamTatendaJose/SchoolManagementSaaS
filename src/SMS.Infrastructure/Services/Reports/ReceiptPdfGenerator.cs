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

                page.Header().Element(h =>
                    BrandedHeader.Compose(h, model.Branding, "Payment Receipt", $"No. {model.ReceiptNumber}"));

                page.Content().PaddingVertical(12).Column(col =>
                {
                    col.Spacing(6);
                    col.Item().Text($"Date: {model.PaymentDate:dd MMM yyyy}");
                    col.Item().Text($"Student: {model.StudentName} ({model.StudentNumber})");
                    col.Item().Text($"Invoice: {model.InvoiceNumber}");
                    col.Item().Text($"Method: {model.PaymentMethod}");

                    col.Item().PaddingTop(8).Background(BrandedHeader.Accent(model.Branding)).Padding(8)
                        .Text($"Amount paid: {model.Currency} {model.Amount.ToString("0.00", CultureInfo.InvariantCulture)}")
                        .FontSize(14).SemiBold().FontColor(Colors.White);

                    col.Item().PaddingTop(4).Text($"Remaining balance: {model.Currency} {model.RemainingBalance.ToString("0.00", CultureInfo.InvariantCulture)}")
                        .SemiBold();
                });

                page.Footer().Element(f => BrandedHeader.Footer(f, model.Branding, "Thank you for your payment."));
            });
        }).GeneratePdf();
    }
}
