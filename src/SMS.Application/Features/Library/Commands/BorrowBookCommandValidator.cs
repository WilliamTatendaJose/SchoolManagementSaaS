using FluentValidation;

namespace SMS.Application.Features.Library.Commands;

public class BorrowBookCommandValidator : AbstractValidator<BorrowBookCommand>
{
    public BorrowBookCommandValidator()
    {
        RuleFor(x => x.BookId)
            .NotEmpty().WithMessage("Book is required");

        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("Student is required");

        RuleFor(x => x.DueDate)
            .GreaterThan(x => x.BorrowedDate ?? DateTime.MinValue)
            .When(x => x.DueDate.HasValue && x.BorrowedDate.HasValue)
            .WithMessage("Due date must be after the borrowed date");
    }
}
