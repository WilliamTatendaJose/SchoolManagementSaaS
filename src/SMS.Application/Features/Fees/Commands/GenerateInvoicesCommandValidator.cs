using FluentValidation;

namespace SMS.Application.Features.Fees.Commands;

public class GenerateInvoicesCommandValidator : AbstractValidator<GenerateInvoicesCommand>
{
    public GenerateInvoicesCommandValidator()
    {
        RuleFor(x => x.AcademicTermId)
            .NotEmpty().WithMessage("Academic term is required");

        RuleFor(x => x.DueDate)
            .NotEmpty().WithMessage("Due date is required");

        RuleFor(x => x.SiblingDiscountPercent)
            .InclusiveBetween(0, 100).WithMessage("Sibling discount percent must be between 0 and 100");
    }
}
