using FluentValidation.TestHelper;
using SMS.Application.Features.Guardians.Commands;
using Xunit;

namespace SMS.Application.Tests.Features.Guardians;

public class LinkGuardianToStudentCommandValidatorTests
{
    private readonly LinkGuardianToStudentCommandValidator _validator = new();

    private static LinkGuardianToStudentCommand ValidCommand() => new()
    {
        GuardianId = Guid.NewGuid(),
        StudentId = Guid.NewGuid(),
        Relationship = "Father"
    };

    [Fact]
    public void Validate_ShouldPass_WhenValidCommand()
    {
        var result = _validator.TestValidate(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldFail_WhenGuardianIdIsEmpty()
    {
        var command = ValidCommand() with { GuardianId = Guid.Empty };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.GuardianId);
    }

    [Fact]
    public void Validate_ShouldFail_WhenStudentIdIsEmpty()
    {
        var command = ValidCommand() with { StudentId = Guid.Empty };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.StudentId);
    }

    [Fact]
    public void Validate_ShouldFail_WhenRelationshipIsEmpty()
    {
        var command = ValidCommand() with { Relationship = "" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Relationship);
    }
}
