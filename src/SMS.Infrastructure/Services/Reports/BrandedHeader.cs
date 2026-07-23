using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SMS.Application.Common.Branding;

namespace SMS.Infrastructure.Services.Reports;

/// <summary>
/// Shared branded header/footer for the school's PDF documents (report cards, receipts,
/// invoices) so they read as one system: optional logo, school name + contact in the
/// primary colour, the document title in the accent colour, and an accent underline.
/// </summary>
internal static class BrandedHeader
{
    public static Color Primary(SchoolBranding b) => Color.FromHex(b.PrimaryColor);
    public static Color Accent(SchoolBranding b) => Color.FromHex(b.AccentColor);

    public static void Compose(IContainer container, SchoolBranding b, string documentTitle, string? subtitle = null)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                if (b.LogoImage is { Length: > 0 })
                {
                    row.ConstantItem(58).Height(58).AlignMiddle().Image(b.LogoImage).FitArea();
                    row.ConstantItem(12);
                }

                row.RelativeItem().Column(info =>
                {
                    info.Item().Text(b.SchoolName).FontSize(17).SemiBold().FontColor(Primary(b));
                    var contact = string.Join("  •  ",
                        new[] { b.AddressLine, b.Phone, b.Email, b.Website }.Where(x => !string.IsNullOrWhiteSpace(x)));
                    if (contact.Length > 0)
                    {
                        info.Item().PaddingTop(1).Text(contact).FontSize(8).FontColor(Colors.Grey.Darken1);
                    }
                });

                row.ConstantItem(170).Column(title =>
                {
                    title.Item().AlignRight().Text(documentTitle).FontSize(13).SemiBold().FontColor(Accent(b));
                    if (!string.IsNullOrWhiteSpace(subtitle))
                    {
                        title.Item().AlignRight().Text(subtitle).FontSize(9).FontColor(Colors.Grey.Darken1);
                    }
                });
            });

            col.Item().PaddingTop(6).LineHorizontal(2).LineColor(Accent(b));
        });
    }

    public static void Footer(IContainer container, SchoolBranding b, string text)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
            col.Item().PaddingTop(3).Row(row =>
            {
                row.RelativeItem().Text(text).FontSize(8).FontColor(Colors.Grey.Darken1);
                row.RelativeItem().AlignRight().Text(b.SchoolName).FontSize(8).FontColor(Colors.Grey.Darken1);
            });
        });
    }
}
