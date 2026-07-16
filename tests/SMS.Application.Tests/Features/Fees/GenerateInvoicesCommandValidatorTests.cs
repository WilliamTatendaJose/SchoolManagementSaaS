using FluentValidation.TestHelper;
using SMS.Application.Features.Fees.Commands;
using Xunit;

namespace SMS.Application.Tests.Features.Fees;

public class GenerateInvoicesCommandValidatorTests
{
    private readonly GenerateInvoicesCommandValidator _validator = new();

    private static GenerateInvoicesCommand ValidCommand() => new()
    {
        AcademicTermId = Guid.NewGuid(),
        DueDate = DateTime.UtcNow.AddDays(30)
    };

    [Fact]
    public void Validate_ShouldPass_WhenValidCommand()
    {
        var result = _validator.TestValidate(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldFail_WhenAcademicTermIdIsEmpty()
    {
        var command = ValidCommand() with { AcademicTermId = Guid.Empty };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.AcademicTermId);
    }

    [Fact]
    public void Validate_ShouldFail_WhenDueDateIsMissing()
    {
        var command = ValidCommand() with { DueDate = default };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.DueDate);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Validate_ShouldFail_WhenSiblingDiscountPercentIsOutOfRange(decimal percent)
    {
        var command = ValidCommand() with { SiblingDiscountPercent = percent };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.SiblingDiscountPercent);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(100)]
    public void Validate_ShouldPass_WhenSiblingDiscountPercentIsInRange(decimal percent)
    {
        var command = ValidCommand() with { SiblingDiscountPercent = percent };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.SiblingDiscountPercent);
    }
}
