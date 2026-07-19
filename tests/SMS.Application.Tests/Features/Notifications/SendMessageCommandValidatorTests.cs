using FluentValidation.TestHelper;
using SMS.Application.Features.Notifications.Commands;
using Xunit;

namespace SMS.Application.Tests.Features.Notifications;

public class SendMessageCommandValidatorTests
{
    private readonly SendMessageCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldPass_ForSpecificStudents()
    {
        var command = new SendMessageCommand
        {
            Content = "Hello",
            Audience = nameof(MessageAudience.SpecificStudents),
            StudentIds = [Guid.NewGuid()]
        };

        _validator.TestValidate(command).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldFail_WhenContentIsEmpty()
    {
        var command = new SendMessageCommand
        {
            Content = "",
            Audience = nameof(MessageAudience.AllActiveStudents)
        };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public void Validate_ShouldFail_WhenSpecificStudentsHasNoIds()
    {
        var command = new SendMessageCommand
        {
            Content = "Hello",
            Audience = nameof(MessageAudience.SpecificStudents),
            StudentIds = []
        };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.StudentIds);
    }

    [Fact]
    public void Validate_ShouldFail_WhenClassAudienceHasNoClassId()
    {
        var command = new SendMessageCommand
        {
            Content = "Hello",
            Audience = nameof(MessageAudience.Class)
        };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.ClassId);
    }
}
