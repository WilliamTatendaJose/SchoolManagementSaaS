using FluentValidation.TestHelper;
using SMS.Application.Features.PaymentPlans.Commands;
using Xunit;

namespace SMS.Application.Tests.Features.PaymentPlans;

public class CreatePaymentPlanCommandValidatorTests
{
    private readonly CreatePaymentPlanCommandValidator _validator = new();

    private static CreatePaymentPlanCommand ValidCommand() => new()
    {
        InvoiceId = Guid.NewGuid(),
        InstallmentCount = 3,
        Frequency = "Monthly"
    };

    [Fact]
    public void Validate_ShouldPass_WhenValid()
    {
        _validator.TestValidate(ValidCommand()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldFail_WhenInvoiceIdEmpty()
    {
        _validator.TestValidate(ValidCommand() with { InvoiceId = Guid.Empty })
            .ShouldHaveValidationErrorFor(x => x.InvoiceId);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    [InlineData(25)]
    public void Validate_ShouldFail_WhenInstallmentCountOutOfRange(int count)
    {
        _validator.TestValidate(ValidCommand() with { InstallmentCount = count })
            .ShouldHaveValidationErrorFor(x => x.InstallmentCount);
    }

    [Fact]
    public void Validate_ShouldFail_WhenFrequencyInvalid()
    {
        _validator.TestValidate(ValidCommand() with { Frequency = "Daily" })
            .ShouldHaveValidationErrorFor(x => x.Frequency);
    }

    [Theory]
    [InlineData("Monthly")]
    [InlineData("weekly")]
    public void Validate_ShouldPass_WhenFrequencyValidAnyCase(string frequency)
    {
        _validator.TestValidate(ValidCommand() with { Frequency = frequency })
            .ShouldNotHaveValidationErrorFor(x => x.Frequency);
    }
}
