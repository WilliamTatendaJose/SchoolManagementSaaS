using FluentAssertions;
using SMS.Application.Common.Branding;
using SMS.Application.Interfaces;
using SMS.Infrastructure.Services.Reports;
using Xunit;

namespace SMS.Infrastructure.Tests;

/// <summary>
/// Smoke tests that the branded PDF generators produce a valid PDF - including with a real
/// embedded logo and custom brand colours (QuestPDF decodes the image at render time, so a
/// bad logo would throw here). Not a visual check; asserts a non-empty %PDF document.
/// </summary>
public class BrandedPdfTests
{
    // A minimal valid 1x1 transparent PNG.
    private static readonly byte[] TinyPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");

    private static SchoolBranding Branding() => new()
    {
        SchoolName = "Test Academy",
        AddressLine = "1 School Rd, Harare",
        Phone = "+263 771 000 000",
        Email = "info@test.ac.zw",
        LogoImage = TinyPng,
        PrimaryColor = "#123456",
        AccentColor = "#AA3311"
    };

    private static void ShouldBePdf(byte[] bytes)
    {
        bytes.Should().NotBeNullOrEmpty();
        // PDF magic number: %PDF
        bytes[..4].Should().Equal((byte)'%', (byte)'P', (byte)'D', (byte)'F');
    }

    [Fact]
    public void Report_card_renders_with_branding_and_logo()
    {
        var pdf = new ReportCardPdfGenerator().Generate(new ReportCardModel
        {
            Branding = Branding(),
            StudentName = "Rufaro Nyathi",
            StudentNumber = "STU-1",
            ClassName = "Form 2",
            TermName = "Term 1",
            GradingScheme = "ZIMSEC",
            OverallAverage = 72.5m,
            OverallGrade = "B",
            ClassRank = 3,
            TotalInClass = 30,
            Subjects =
            [
                new ReportCardSubjectLine { SubjectName = "Maths", Percentage = 80, Grade = "A" },
                new ReportCardSubjectLine { SubjectName = "English", Percentage = 65, Grade = "B" }
            ],
            ClassTeacherComment = "Good progress.",
            HeadComment = "Keep it up."
        });

        ShouldBePdf(pdf);
    }

    [Fact]
    public void Receipt_renders_with_branding()
    {
        var pdf = new ReceiptPdfGenerator().Generate(new ReceiptModel
        {
            Branding = Branding(),
            ReceiptNumber = "RCT-1",
            StudentName = "Rufaro Nyathi",
            StudentNumber = "STU-1",
            InvoiceNumber = "INV-1",
            Amount = 150m,
            Currency = "USD",
            PaymentMethod = "Cash",
            PaymentDate = new DateTime(2026, 2, 1),
            RemainingBalance = 50m
        });

        ShouldBePdf(pdf);
    }

    [Fact]
    public void Invoice_renders_with_branding_lines_and_totals()
    {
        var pdf = new InvoicePdfGenerator().Generate(new InvoiceModel
        {
            Branding = Branding(),
            InvoiceNumber = "INV-1",
            StudentName = "Rufaro Nyathi",
            StudentNumber = "STU-1",
            ClassName = "Form 2",
            TermName = "Term 1",
            Currency = "USD",
            InvoiceDate = new DateTime(2026, 1, 15),
            DueDate = new DateTime(2026, 2, 15),
            Lines =
            [
                new InvoiceLine { Description = "Tuition", Quantity = 1, Amount = 500, LineTotal = 500 },
                new InvoiceLine { Description = "Levy", Quantity = 1, Amount = 100, LineTotal = 100 }
            ],
            Subtotal = 600,
            Discount = 60,
            Paid = 200,
            Balance = 340,
            Notes = "Payable via Paynow."
        });

        ShouldBePdf(pdf);
    }

    [Fact]
    public void Generators_tolerate_no_logo_and_default_colours()
    {
        var plain = new SchoolBranding { SchoolName = "No Logo School" };
        var pdf = new InvoicePdfGenerator().Generate(new InvoiceModel { Branding = plain, InvoiceNumber = "INV-2" });
        ShouldBePdf(pdf);
    }
}
