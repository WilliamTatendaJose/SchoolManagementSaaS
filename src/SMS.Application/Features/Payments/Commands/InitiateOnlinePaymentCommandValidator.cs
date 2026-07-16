using FluentValidation;

namespace SMS.Application.Features.Payments.Commands;

public class InitiateOnlinePaymentCommandValidator : AbstractValidator<InitiateOnlinePaymentCommand>
{
    public InitiateOnlinePaymentCommandValidator()
    {
        RuleFor(x => x.InvoiceId)
            .NotEmpty().WithMessage("Invoice is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0).When(x => x.Amount.HasValue)
            .WithMessage("Amount must be greater than zero");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("A valid email address is required");
    }
}
