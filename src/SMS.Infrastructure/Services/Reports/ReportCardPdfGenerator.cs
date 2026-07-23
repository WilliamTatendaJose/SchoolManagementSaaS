using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SMS.Application.Interfaces;

namespace SMS.Infrastructure.Services.Reports;

/// <summary>
/// Renders a report card to PDF with QuestPDF. Uses the free Community license, valid for
/// companies/individuals under the QuestPDF revenue threshold.
/// </summary>
public class ReportCardPdfGenerator : IReportCardGenerator
{
    static ReportCardPdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Generate(ReportCardModel model)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Element(h =>
                    BrandedHeader.Compose(h, model.Branding, "Report Card", $"{model.TermName} • {model.GradingScheme}"));

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Spacing(8);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Name: {model.StudentName}");
                        row.RelativeItem().Text($"No: {model.StudentNumber}");
                        row.RelativeItem().Text($"Class: {model.ClassName}");
                    });

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        var accent = BrandedHeader.Accent(model.Branding);
                        table.Header(header =>
                        {
                            header.Cell().Element(c => HeaderCell(c, accent)).Text("Subject");
                            header.Cell().Element(c => HeaderCell(c, accent)).AlignRight().Text("Percentage");
                            header.Cell().Element(c => HeaderCell(c, accent)).AlignRight().Text("Grade");
                        });

                        foreach (var subject in model.Subjects)
                        {
                            table.Cell().Element(BodyCell).Text(subject.SubjectName);
                            table.Cell().Element(BodyCell).AlignRight().Text($"{subject.Percentage:0.0}%");
                            table.Cell().Element(BodyCell).AlignRight().Text(subject.Grade);
                        }
                    });

                    col.Item().PaddingTop(6)
                        .Text($"Overall average: {model.OverallAverage:0.0}%  (Grade {model.OverallGrade})")
                        .SemiBold();

                    if (model.TotalInClass > 0)
                    {
                        col.Item().Text($"Position in class: {model.ClassRank} of {model.TotalInClass}");
                    }

                    if (!string.IsNullOrWhiteSpace(model.ClassTeacherComment))
                    {
                        col.Item().PaddingTop(6).Text($"Class teacher: {model.ClassTeacherComment}");
                    }

                    if (!string.IsNullOrWhiteSpace(model.HeadComment))
                    {
                        col.Item().Text($"Head: {model.HeadComment}");
                    }
                });

                page.Footer().Element(f =>
                    BrandedHeader.Footer(f, model.Branding, $"Generated {model.GeneratedAt:dd MMM yyyy}"));
            });
        }).GeneratePdf();
    }

    private static IContainer HeaderCell(IContainer container, Color accent) =>
        container.BorderBottom(1.5f).BorderColor(accent).PaddingVertical(4)
            .DefaultTextStyle(x => x.SemiBold().FontColor(accent));

    private static IContainer BodyCell(IContainer container) =>
        container.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3);
}
