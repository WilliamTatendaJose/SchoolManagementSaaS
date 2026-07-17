using FluentValidation;

namespace SMS.Application.Features.Hostel.Commands;

public class CreateDormitoryCommandValidator : AbstractValidator<CreateDormitoryCommand>
{
    public CreateDormitoryCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Dormitory name is required")
            .MaximumLength(100).WithMessage("Dormitory name must not exceed 100 characters");

        RuleFor(x => x.Capacity)
            .GreaterThan(0).WithMessage("Capacity must be greater than zero");

        RuleFor(x => x.Gender)
            .Must(g => g is null || g is "Male" or "Female")
            .WithMessage("Gender must be Male, Female, or left blank");
    }
}
