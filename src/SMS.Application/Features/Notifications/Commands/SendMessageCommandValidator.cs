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

        RuleFor(x => x.Audience)
            .Must(a => Enum.TryParse<MessageAudience>(a, out _))
            .WithMessage("Audience must be one of: SpecificStudents, Class, AllActiveStudents");

        RuleFor(x => x.StudentIds)
            .NotEmpty().When(x => x.Audience == nameof(MessageAudience.SpecificStudents))
            .WithMessage("At least one student is required for a specific-students message");

        RuleFor(x => x.ClassId)
            .NotEmpty().When(x => x.Audience == nameof(MessageAudience.Class))
            .WithMessage("A class is required for a class message");
    }
}
