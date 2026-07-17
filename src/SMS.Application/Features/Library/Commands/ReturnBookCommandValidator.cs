using FluentValidation;

namespace SMS.Application.Features.Library.Commands;

public class ReturnBookCommandValidator : AbstractValidator<ReturnBookCommand>
{
    public ReturnBookCommandValidator()
    {
        RuleFor(x => x.LoanId)
            .NotEmpty().WithMessage("Loan is required");
    }
}
