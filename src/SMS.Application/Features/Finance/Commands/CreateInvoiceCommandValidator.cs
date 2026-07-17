using FluentValidation;

namespace SMS.Application.Features.Finance.Commands;

public class CreateInvoiceCommandValidator : AbstractValidator<CreateInvoiceCommand>
{
    public CreateInvoiceCommandValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("Student is required");

        RuleFor(x => x.AcademicTermId)
            .NotEmpty().WithMessage("Academic term is required");

        RuleFor(x => x.DueDate)
            .NotEmpty().WithMessage("Due date is required");

        RuleFor(x => x.DiscountAmount)
            .GreaterThanOrEqualTo(0).When(x => x.DiscountAmount.HasValue)
            .WithMessage("Discount cannot be negative");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("An invoice must have at least one item");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Description)
                .NotEmpty().WithMessage("Item description is required");

            item.RuleFor(i => i.Amount)
                .GreaterThan(0).WithMessage("Item amount must be greater than zero");

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("Item quantity must be greater than zero");
        });
    }
}
