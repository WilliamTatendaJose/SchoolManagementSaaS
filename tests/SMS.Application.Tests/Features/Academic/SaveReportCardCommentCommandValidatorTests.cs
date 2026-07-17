using FluentAssertions;
using FluentValidation.TestHelper;
using SMS.Application.Features.Academic.Commands;
using Xunit;

namespace SMS.Application.Tests.Features.Academic;

public class SaveReportCardCommentCommandValidatorTests
{
    private readonly SaveReportCardCommentCommandValidator _validator = new();

    private static SaveReportCardCommentCommand ValidCommand() => new()
    {
        StudentId = Guid.NewGuid(),
        AcademicTermId = Guid.NewGuid(),
        ClassTeacherComment = "Doing well."
    };

    [Fact]
    public void Validate_ShouldPass_WhenValid()
    {
        _validator.TestValidate(ValidCommand()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldFail_WhenStudentIdEmpty()
    {
        _validator.TestValidate(ValidCommand() with { StudentId = Guid.Empty })
            .ShouldHaveValidationErrorFor(x => x.StudentId);
    }

    [Fact]
    public void Validate_ShouldFail_WhenAcademicTermIdEmpty()
    {
        _validator.TestValidate(ValidCommand() with { AcademicTermId = Guid.Empty })
            .ShouldHaveValidationErrorFor(x => x.AcademicTermId);
    }

    [Fact]
    public void Validate_ShouldFail_WhenNeitherCommentProvided()
    {
        var result = _validator.Validate(ValidCommand() with { ClassTeacherComment = null, HeadComment = null });
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ShouldPass_WhenOnlyHeadCommentProvided()
    {
        _validator.TestValidate(ValidCommand() with { ClassTeacherComment = null, HeadComment = "Excellent term." })
            .ShouldNotHaveAnyValidationErrors();
    }
}
