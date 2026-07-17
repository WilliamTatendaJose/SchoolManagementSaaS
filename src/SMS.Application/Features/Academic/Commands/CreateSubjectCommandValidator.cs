using FluentValidation;

namespace SMS.Application.Features.Academic.Commands;

public class CreateSubjectCommandValidator : AbstractValidator<CreateSubjectCommand>
{
    public CreateSubjectCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Subject name is required")
            .MaximumLength(100).WithMessage("Subject name must not exceed 100 characters");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Subject code is required")
            .MaximumLength(20).WithMessage("Subject code must not exceed 20 characters");
    }
}
