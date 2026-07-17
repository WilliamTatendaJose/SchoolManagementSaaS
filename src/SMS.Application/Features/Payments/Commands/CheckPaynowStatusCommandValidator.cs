using FluentValidation;

namespace SMS.Application.Features.Payments.Commands;

public class CheckPaynowStatusCommandValidator : AbstractValidator<CheckPaynowStatusCommand>
{
    public CheckPaynowStatusCommandValidator()
    {
        RuleFor(x => x.PaymentId)
            .NotEmpty().WithMessage("Payment is required");
    }
}
