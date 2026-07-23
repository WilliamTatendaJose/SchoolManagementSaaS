using FluentValidation.TestHelper;
using SMS.Application.Features.Lms.Commands;
using Xunit;

namespace SMS.Application.Tests.Features.Lms;

public class CreateCourseMaterialCommandValidatorTests
{
    private readonly CreateCourseMaterialCommandValidator _validator = new();

    private static CreateCourseMaterialCommand LinkCommand() => new()
    {
        ClassId = Guid.NewGuid(),
        SubjectId = Guid.NewGuid(),
        Title = "Chapter 3 notes",
        Url = "https://example.com/notes"
    };

    private static CreateCourseMaterialCommand FileCommand() => new()
    {
        ClassId = Guid.NewGuid(),
        SubjectId = Guid.NewGuid(),
        Title = "Worksheet",
        AttachmentFileName = "worksheet.pdf",
        AttachmentContent = [1, 2, 3]
    };

    [Fact]
    public void Validate_ShouldPass_ForALink()
    {
        _validator.TestValidate(LinkCommand()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldPass_ForAFile()
    {
        _validator.TestValidate(FileCommand()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldFail_WhenNeitherLinkNorFileProvided()
    {
        _validator.TestValidate(LinkCommand() with { Url = null })
            .ShouldHaveValidationErrorFor(x => x);
    }

    [Fact]
    public void Validate_ShouldFail_WhenClassIdEmpty()
    {
        _validator.TestValidate(LinkCommand() with { ClassId = Guid.Empty })
            .ShouldHaveValidationErrorFor(x => x.ClassId);
    }

    [Fact]
    public void Validate_ShouldFail_WhenTitleEmpty()
    {
        _validator.TestValidate(LinkCommand() with { Title = "" })
            .ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Validate_ShouldFail_WhenUrlIsNotAValidUrl()
    {
        _validator.TestValidate(LinkCommand() with { Url = "not a url" })
            .ShouldHaveValidationErrorFor(x => x.Url);
    }
}
