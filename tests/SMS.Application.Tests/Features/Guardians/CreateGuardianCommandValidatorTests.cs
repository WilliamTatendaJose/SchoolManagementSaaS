using FluentAssertions;
using FluentValidation.TestHelper;
using SMS.Application.Features.Guardians.Commands;
using Xunit;

namespace SMS.Application.Tests.Features.Guardians;

public class CreateGuardianCommandValidatorTests
{
    private readonly CreateGuardianCommandValidator _validator = new();

    private static CreateGuardianCommand ValidCommand() => new()
    {
        FirstName = "Jane",
        LastName = "Doe",
        Gender = "Female",
        Phone = "+263771234567"
    };

    [Fact]
    public void Validate_ShouldPass_WhenValidCommand()
    {
        var result = _validator.TestValidate(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldFail_WhenFirstNameIsEmpty()
    {
        var command = ValidCommand() with { FirstName = "" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void Validate_ShouldFail_WhenLastNameIsEmpty()
    {
        var command = ValidCommand() with { LastName = "" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.LastName);
    }

    [Fact]
    public void Validate_ShouldFail_WhenGenderIsInvalid()
    {
        var command = ValidCommand() with { Gender = "InvalidGender" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Gender);
    }

    [Fact]
    public void Validate_ShouldFail_WhenPhoneIsEmpty()
    {
        var command = ValidCommand() with { Phone = "" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Phone);
    }

    [Fact]
    public void Validate_ShouldFail_WhenEmailIsInvalid()
    {
        var command = ValidCommand() with { Email = "not-an-email" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_ShouldFail_WhenLinkingWithoutRelationship()
    {
        var command = ValidCommand() with
        {
            LinkToStudent = new GuardianStudentLink
            {
                StudentId = Guid.NewGuid(),
                Relationship = ""
            }
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor("LinkToStudent.Relationship");
    }

    [Fact]
    public void Validate_ShouldPass_WhenLinkingWithValidRelationship()
    {
        var command = ValidCommand() with
        {
            LinkToStudent = new GuardianStudentLink
            {
                StudentId = Guid.NewGuid(),
                Relationship = "Mother",
                IsPrimaryContact = true
            }
        };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
