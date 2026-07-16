using FluentValidation.TestHelper;
using SMS.Application.Features.Payments.Commands;
using Xunit;

namespace SMS.Application.Tests.Features.Payments;

public class InitiateOnlinePaymentCommandValidatorTests
{
    private readonly InitiateOnlinePaymentCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldPass_WhenInvoiceIdProvided()
    {
        var result = _validator.TestValidate(new InitiateOnlinePaymentCommand { InvoiceId = Guid.NewGuid() });

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldFail_WhenInvoiceIdIsEmpty()
    {
        var result = _validator.TestValidate(new InitiateOnlinePaymentCommand { InvoiceId = Guid.Empty });

        result.ShouldHaveValidationErrorFor(x => x.InvoiceId);
    }

    [Fact]
    public void Validate_ShouldFail_WhenAmountIsNotPositive()
    {
        var command = new InitiateOnlinePaymentCommand { InvoiceId = Guid.NewGuid(), Amount = 0 };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void Validate_ShouldFail_WhenEmailIsInvalid()
    {
        var command = new InitiateOnlinePaymentCommand { InvoiceId = Guid.NewGuid(), Email = "nope" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }
}
