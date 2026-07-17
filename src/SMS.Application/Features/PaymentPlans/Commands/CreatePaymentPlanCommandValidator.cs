using FluentValidation;

namespace SMS.Application.Features.PaymentPlans.Commands;

public class CreatePaymentPlanCommandValidator : AbstractValidator<CreatePaymentPlanCommand>
{
    private static readonly string[] ValidFrequencies = ["Monthly", "Weekly"];

    public CreatePaymentPlanCommandValidator()
    {
        RuleFor(x => x.InvoiceId)
            .NotEmpty().WithMessage("Invoice is required");

        RuleFor(x => x.InstallmentCount)
            .InclusiveBetween(2, 24)
            .WithMessage("Installment count must be between 2 and 24");

        RuleFor(x => x.Frequency)
            .Must(f => ValidFrequencies.Contains(f, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Frequency must be Monthly or Weekly");
    }
}
