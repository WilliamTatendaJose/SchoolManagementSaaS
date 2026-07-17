using FluentValidation;

namespace SMS.Application.Features.PaymentPlans.Commands;

public class MarkInstallmentPaidCommandValidator : AbstractValidator<MarkInstallmentPaidCommand>
{
    public MarkInstallmentPaidCommandValidator()
    {
        RuleFor(x => x.InstallmentId)
            .NotEmpty().WithMessage("Installment is required");
    }
}
