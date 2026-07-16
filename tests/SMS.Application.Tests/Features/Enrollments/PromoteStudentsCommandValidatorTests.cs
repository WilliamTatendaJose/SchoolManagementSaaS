using FluentValidation.TestHelper;
using SMS.Application.Features.Enrollments.Commands;
using Xunit;

namespace SMS.Application.Tests.Features.Enrollments;

public class PromoteStudentsCommandValidatorTests
{
    private readonly PromoteStudentsCommandValidator _validator = new();

    private static PromoteStudentsCommand ValidCommand() => new()
    {
        ToClassId = Guid.NewGuid(),
        ToAcademicYearId = Guid.NewGuid(),
        StudentIds = [Guid.NewGuid(), Guid.NewGuid()]
    };

    [Fact]
    public void Validate_ShouldPass_WhenValidCommand()
    {
        var result = _validator.TestValidate(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldFail_WhenToClassIdIsEmpty()
    {
        var command = ValidCommand() with { ToClassId = Guid.Empty };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ToClassId);
    }

    [Fact]
    public void Validate_ShouldFail_WhenToAcademicYearIdIsEmpty()
    {
        var command = ValidCommand() with { ToAcademicYearId = Guid.Empty };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ToAcademicYearId);
    }

    [Fact]
    public void Validate_ShouldFail_WhenStudentIdsIsEmpty()
    {
        var command = ValidCommand() with { StudentIds = [] };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.StudentIds);
    }
}
