using FluentAssertions;
using SMS.Domain.Entities;
using Xunit;

namespace SMS.Domain.Tests.Entities;

public class InvoiceTests
{
    [Fact]
    public void Invoice_Balance_ShouldCalculateCorrectly()
    {
        // Arrange
        var invoice = new Invoice
        {
            TotalAmount = 1000m,
            DiscountAmount = 100m,
            PaidAmount = 500m
        };

        // Act
        var balance = invoice.Balance;

        // Assert
        balance.Should().Be(400m);
    }

    [Fact]
    public void Invoice_IsPaid_ShouldReturnTrue_WhenBalanceIsZero()
    {
        // Arrange
        var invoice = new Invoice
        {
            TotalAmount = 1000m,
            DiscountAmount = 0m,
            PaidAmount = 1000m
        };

        // Act & Assert
        invoice.IsPaid.Should().BeTrue();
    }

    [Fact]
    public void Invoice_IsPaid_ShouldReturnFalse_WhenBalanceIsPositive()
    {
        // Arrange
        var invoice = new Invoice
        {
            TotalAmount = 1000m,
            DiscountAmount = 0m,
            PaidAmount = 500m
        };

        // Act & Assert
        invoice.IsPaid.Should().BeFalse();
    }

    [Fact]
    public void Invoice_IsPaid_ShouldReturnTrue_WhenOverpaid()
    {
        // Arrange
        var invoice = new Invoice
        {
            TotalAmount = 1000m,
            DiscountAmount = 0m,
            PaidAmount = 1500m
        };

        // Act & Assert
        invoice.IsPaid.Should().BeTrue();
    }
}
