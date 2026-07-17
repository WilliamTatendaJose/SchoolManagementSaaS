using FluentValidation;

namespace SMS.Application.Features.Academic.Commands;

public class CreateClassCommandValidator : AbstractValidator<CreateClassCommand>
{
    public CreateClassCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Class name is required")
            .MaximumLength(100).WithMessage("Class name must not exceed 100 characters");

        RuleFor(x => x.Code)
            .MaximumLength(20).WithMessage("Class code must not exceed 20 characters");

        RuleFor(x => x.Level)
            .GreaterThanOrEqualTo(0).WithMessage("Level cannot be negative");

        RuleFor(x => x.Capacity)
            .GreaterThanOrEqualTo(0).WithMessage("Capacity cannot be negative");
    }
}
