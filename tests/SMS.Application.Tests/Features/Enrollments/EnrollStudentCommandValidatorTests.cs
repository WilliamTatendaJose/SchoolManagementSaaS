using FluentValidation.TestHelper;
using SMS.Application.Features.Enrollments.Commands;
using Xunit;

namespace SMS.Application.Tests.Features.Enrollments;

public class EnrollStudentCommandValidatorTests
{
    private readonly EnrollStudentCommandValidator _validator = new();

    private static EnrollStudentCommand ValidCommand() => new()
    {
        StudentId = Guid.NewGuid(),
        ClassId = Guid.NewGuid(),
        AcademicYearId = Guid.NewGuid()
    };

    [Fact]
    public void Validate_ShouldPass_WhenValidCommand()
    {
        var result = _validator.TestValidate(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldFail_WhenStudentIdIsEmpty()
    {
        var command = ValidCommand() with { StudentId = Guid.Empty };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.StudentId);
    }

    [Fact]
    public void Validate_ShouldFail_WhenClassIdIsEmpty()
    {
        var command = ValidCommand() with { ClassId = Guid.Empty };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ClassId);
    }

    [Fact]
    public void Validate_ShouldFail_WhenAcademicYearIdIsEmpty()
    {
        var command = ValidCommand() with { AcademicYearId = Guid.Empty };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.AcademicYearId);
    }

    [Fact]
    public void Validate_ShouldFail_WhenEnrollmentDateIsInFuture()
    {
        var command = ValidCommand() with { EnrollmentDate = DateTime.UtcNow.AddDays(10) };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.EnrollmentDate);
    }
}
