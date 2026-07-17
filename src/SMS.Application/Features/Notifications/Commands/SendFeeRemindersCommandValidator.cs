using FluentValidation;

namespace SMS.Application.Features.Notifications.Commands;

public class SendFeeRemindersCommandValidator : AbstractValidator<SendFeeRemindersCommand>
{
    public SendFeeRemindersCommandValidator()
    {
        RuleFor(x => x.Channel)
            .NotEmpty().WithMessage("Channel is required");
    }
}
