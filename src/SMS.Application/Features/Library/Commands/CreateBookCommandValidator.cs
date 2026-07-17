using FluentValidation;

namespace SMS.Application.Features.Library.Commands;

public class CreateBookCommandValidator : AbstractValidator<CreateBookCommand>
{
    public CreateBookCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(300);

        RuleFor(x => x.Author)
            .NotEmpty().WithMessage("Author is required")
            .MaximumLength(200);

        RuleFor(x => x.Isbn)
            .MaximumLength(20);

        RuleFor(x => x.Category)
            .MaximumLength(100);

        RuleFor(x => x.TotalCopies)
            .GreaterThan(0).WithMessage("Total copies must be greater than zero");
    }
}
