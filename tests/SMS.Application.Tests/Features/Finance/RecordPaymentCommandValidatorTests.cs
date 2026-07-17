using FluentValidation.TestHelper;
using SMS.Application.Features.Finance.Commands;
using Xunit;

namespace SMS.Application.Tests.Features.Finance;

public class RecordPaymentCommandValidatorTests
{
    private readonly RecordPaymentCommandValidator _validator = new();

    private static RecordPaymentCommand ValidCommand() => new()
    {
        InvoiceId = Guid.NewGuid(),
        Amount = 100m,
        PaymentMethod = "MobileMoney"
    };

    [Fact]
    public void Validate_ShouldPass_WhenValid()
    {
        _validator.TestValidate(ValidCommand()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public void Validate_ShouldFail_WhenAmountNotPositive(decimal amount)
    {
        _validator.TestValidate(ValidCommand() with { Amount = amount })
            .ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void Validate_ShouldFail_WhenPaymentMethodInvalid()
    {
        _validator.TestValidate(ValidCommand() with { PaymentMethod = "Bitcoin" })
            .ShouldHaveValidationErrorFor(x => x.PaymentMethod);
    }

    [Fact]
    public void Validate_ShouldFail_WhenInvoiceIdEmpty()
    {
        _validator.TestValidate(ValidCommand() with { InvoiceId = Guid.Empty })
            .ShouldHaveValidationErrorFor(x => x.InvoiceId);
    }
}
