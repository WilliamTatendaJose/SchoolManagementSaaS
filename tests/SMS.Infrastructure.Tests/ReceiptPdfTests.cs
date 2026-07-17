using System.Text;
using FluentAssertions;
using SMS.Application.Interfaces;
using SMS.Infrastructure.Services.Reports;
using Xunit;

namespace SMS.Infrastructure.Tests;

public class ReceiptPdfTests
{
    [Fact]
    public void Generator_produces_a_valid_pdf_receipt()
    {
        var model = new ReceiptModel
        {
            SchoolName = "Finance School",
            ReceiptNumber = "RCP-2026-000001",
            StudentName = "Dave Student",
            StudentNumber = "S-DEF",
            InvoiceNumber = "INV-DEF",
            Amount = 60m,
            Currency = "USD",
            PaymentMethod = "Cash",
            PaymentDate = new DateTime(2026, 3, 2),
            RemainingBalance = 340m
        };

        var pdf = new ReceiptPdfGenerator().Generate(model);

        pdf.Should().NotBeEmpty();
        Encoding.ASCII.GetString(pdf, 0, 4).Should().Be("%PDF");
    }
}
