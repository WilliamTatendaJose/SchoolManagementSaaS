using FluentValidation.TestHelper;
using SMS.Application.Features.Users.Commands;
using Xunit;

namespace SMS.Application.Tests.Features.Users;

public class ChangePasswordCommandValidatorTests
{
    private readonly ChangePasswordCommandValidator _validator = new();

    private static ChangePasswordCommand ValidCommand() => new()
    {
        UserId = Guid.NewGuid(),
        CurrentPassword = "OldPass1",
        NewPassword = "NewPass1"
    };

    [Fact]
    public void Validate_ShouldPass_WhenValid()
    {
        _validator.TestValidate(ValidCommand()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("short1")]      // too short
    [InlineData("alphabetic")] // no digit
    [InlineData("12345678")]   // no letter
    public void Validate_ShouldFail_WhenNewPasswordWeak(string newPassword)
    {
        _validator.TestValidate(ValidCommand() with { NewPassword = newPassword })
            .ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void Validate_ShouldFail_WhenNewPasswordEqualsCurrent()
    {
        var command = ValidCommand() with { CurrentPassword = "SamePass1", NewPassword = "SamePass1" };
        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.NewPassword);
    }
}
