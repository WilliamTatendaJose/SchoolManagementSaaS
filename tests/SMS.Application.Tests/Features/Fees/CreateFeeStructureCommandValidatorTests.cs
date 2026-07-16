using FluentValidation.TestHelper;
using SMS.Application.Features.Fees.Commands;
using Xunit;

namespace SMS.Application.Tests.Features.Fees;

public class CreateFeeStructureCommandValidatorTests
{
    private readonly CreateFeeStructureCommandValidator _validator = new();

    private static CreateFeeStructureCommand ValidCommand() => new()
    {
        ClassId = Guid.NewGuid(),
        AcademicYearId = Guid.NewGuid(),
        Name = "Tuition",
        FeeType = "Tuition",
        Amount = 500m
    };

    [Fact]
    public void Validate_ShouldPass_WhenValidCommand()
    {
        var result = _validator.TestValidate(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldFail_WhenClassIdIsEmpty()
    {
        var command = ValidCommand() with { ClassId = Guid.Empty };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ClassId);
    }

    [Fact]
    public void Validate_ShouldFail_WhenNameIsEmpty()
    {
        var command = ValidCommand() with { Name = "" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_ShouldFail_WhenFeeTypeIsEmpty()
    {
        var command = ValidCommand() with { FeeType = "" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.FeeType);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Validate_ShouldFail_WhenAmountIsNotPositive(decimal amount)
    {
        var command = ValidCommand() with { Amount = amount };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }
}
