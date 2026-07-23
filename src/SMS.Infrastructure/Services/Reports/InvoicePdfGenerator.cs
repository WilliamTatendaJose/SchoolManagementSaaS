using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SMS.Application.Interfaces;

namespace SMS.Infrastructure.Services.Reports;

/// <summary>
/// Renders a fee invoice to a branded PDF with QuestPDF (Community license).
/// </summary>
public class InvoicePdfGenerator : IInvoiceGenerator
{
    static InvoicePdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Generate(InvoiceModel model)
    {
        var accent = BrandedHeader.Accent(model.Branding);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Element(h =>
                    BrandedHeader.Compose(h, model.Branding, "Invoice", $"No. {model.InvoiceNumber}"));

                page.Content().PaddingVertical(12).Column(col =>
                {
                    col.Spacing(10);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Billed to").FontSize(8).FontColor(Colors.Grey.Darken1);
                            c.Item().Text(model.StudentName).SemiBold();
                            c.Item().Text($"{model.StudentNumber} • {model.ClassName}").FontSize(9).FontColor(Colors.Grey.Darken1);
                        });
                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().AlignRight().Text($"Term: {model.TermName}");
                            c.Item().AlignRight().Text($"Invoice date: {model.InvoiceDate:dd MMM yyyy}").FontSize(9);
                            c.Item().AlignRight().Text($"Due date: {model.DueDate:dd MMM yyyy}").FontSize(9);
                        });
                    });

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(5);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(c => HeaderCell(c, accent)).Text("Description");
                            header.Cell().Element(c => HeaderCell(c, accent)).AlignCenter().Text("Qty");
                            header.Cell().Element(c => HeaderCell(c, accent)).AlignRight().Text("Amount");
                            header.Cell().Element(c => HeaderCell(c, accent)).AlignRight().Text("Total");
                        });

                        foreach (var line in model.Lines)
                        {
                            table.Cell().Element(BodyCell).Text(line.Description);
                            table.Cell().Element(BodyCell).AlignCenter().Text(line.Quantity.ToString());
                            table.Cell().Element(BodyCell).AlignRight().Text(Money(line.Amount, model.Currency));
                            table.Cell().Element(BodyCell).AlignRight().Text(Money(line.LineTotal, model.Currency));
                        }
                    });

                    col.Item().AlignRight().Width(260).Column(totals =>
                    {
                        totals.Spacing(3);
                        TotalRow(totals, "Subtotal", Money(model.Subtotal, model.Currency));
                        if (model.Discount > 0) TotalRow(totals, "Discount", "-" + Money(model.Discount, model.Currency));
                        if (model.Paid > 0) TotalRow(totals, "Paid", "-" + Money(model.Paid, model.Currency));
                        totals.Item().PaddingTop(3).Background(accent).Padding(6).Row(row =>
                        {
                            row.RelativeItem().Text("Balance due").FontColor(Colors.White).SemiBold();
                            row.RelativeItem().AlignRight().Text(Money(model.Balance, model.Currency)).FontColor(Colors.White).SemiBold();
                        });
                    });

                    if (!string.IsNullOrWhiteSpace(model.Notes))
                    {
                        col.Item().PaddingTop(6).Text($"Notes: {model.Notes}").FontSize(9).FontColor(Colors.Grey.Darken1);
                    }
                });

                page.Footer().Element(f =>
                    BrandedHeader.Footer(f, model.Branding, $"Generated {DateTime.UtcNow:dd MMM yyyy}"));
            });
        }).GeneratePdf();
    }

    private static string Money(decimal amount, string currency) =>
        $"{currency} {amount.ToString("0.00", CultureInfo.InvariantCulture)}";

    private static void TotalRow(ColumnDescriptor col, string label, string value) =>
        col.Item().Row(row =>
        {
            row.RelativeItem().Text(label).FontColor(Colors.Grey.Darken2);
            row.RelativeItem().AlignRight().Text(value);
        });

    private static IContainer HeaderCell(IContainer container, Color accent) =>
        container.BorderBottom(1.5f).BorderColor(accent).PaddingVertical(4)
            .DefaultTextStyle(x => x.SemiBold().FontColor(accent));

    private static IContainer BodyCell(IContainer container) =>
        container.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4);
}
