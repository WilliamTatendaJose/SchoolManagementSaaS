using FluentValidation;

namespace SMS.Application.Features.Notifications.Commands;

public class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.Channel)
            .NotEmpty().WithMessage("Channel is required");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Message content is required")
            .MaximumLength(2000).WithMessage("Message content must not exceed 2000 characters");

        RuleFor(x => x.StudentIds)
            .NotEmpty().When(x => x.Audience == MessageAudience.SpecificStudents)
            .WithMessage("At least one student is required for a specific-students message");

        RuleFor(x => x.ClassId)
            .NotEmpty().When(x => x.Audience == MessageAudience.Class)
            .WithMessage("A class is required for a class message");
    }
}
