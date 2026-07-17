using FluentValidation;

namespace SMS.Application.Features.Finance.Commands;

public class RecordPaymentCommandValidator : AbstractValidator<RecordPaymentCommand>
{
    private static readonly string[] ValidMethods = ["Cash", "MobileMoney", "BankTransfer", "Card", "Cheque"];

    public RecordPaymentCommandValidator()
    {
        RuleFor(x => x.InvoiceId)
            .NotEmpty().WithMessage("Invoice is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Payment amount must be greater than zero");

        RuleFor(x => x.PaymentMethod)
            .NotEmpty().WithMessage("Payment method is required")
            .Must(m => ValidMethods.Contains(m))
            .WithMessage("Payment method must be Cash, MobileMoney, BankTransfer, Card, or Cheque");
    }
}
