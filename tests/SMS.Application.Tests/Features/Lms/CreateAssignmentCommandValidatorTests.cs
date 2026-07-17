using FluentValidation.TestHelper;
using SMS.Application.Features.Lms.Commands;
using Xunit;

namespace SMS.Application.Tests.Features.Lms;

public class CreateAssignmentCommandValidatorTests
{
    private readonly CreateAssignmentCommandValidator _validator = new();

    private static CreateAssignmentCommand ValidCommand() => new()
    {
        ClassId = Guid.NewGuid(),
        SubjectId = Guid.NewGuid(),
        AcademicTermId = Guid.NewGuid(),
        Title = "Homework 1",
        DueDate = new DateTime(2026, 3, 1)
    };

    [Fact]
    public void Validate_ShouldPass_WhenValid()
    {
        _validator.TestValidate(ValidCommand()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldFail_WhenClassIdEmpty()
    {
        _validator.TestValidate(ValidCommand() with { ClassId = Guid.Empty })
            .ShouldHaveValidationErrorFor(x => x.ClassId);
    }

    [Fact]
    public void Validate_ShouldFail_WhenTitleEmpty()
    {
        _validator.TestValidate(ValidCommand() with { Title = "" })
            .ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Validate_ShouldFail_WhenDueDateDefault()
    {
        _validator.TestValidate(ValidCommand() with { DueDate = default })
            .ShouldHaveValidationErrorFor(x => x.DueDate);
    }
}
