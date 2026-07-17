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

                page.Header().Column(col =>
                {
                    col.Item().Text(model.SchoolName).FontSize(18).SemiBold();
                    col.Item().Text("Student Report Card").FontSize(13);
                    col.Item().PaddingTop(2).Text($"{model.TermName}  •  {model.GradingScheme} grading");
                });

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

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("Subject");
                            header.Cell().Element(HeaderCell).AlignRight().Text("Percentage");
                            header.Cell().Element(HeaderCell).AlignRight().Text("Grade");
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

                page.Footer().AlignCenter().Text($"Generated {model.GeneratedAt:dd MMM yyyy}").FontSize(9);
            });
        }).GeneratePdf();
    }

    private static IContainer HeaderCell(IContainer container) =>
        container.BorderBottom(1).PaddingVertical(4).DefaultTextStyle(x => x.SemiBold());

    private static IContainer BodyCell(IContainer container) =>
        container.BorderBottom(0.5f).PaddingVertical(3);
}
