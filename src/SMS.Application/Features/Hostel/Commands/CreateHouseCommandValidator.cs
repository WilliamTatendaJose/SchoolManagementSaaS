using FluentValidation;

namespace SMS.Application.Features.Hostel.Commands;

public class CreateHouseCommandValidator : AbstractValidator<CreateHouseCommand>
{
    public CreateHouseCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("House name is required")
            .MaximumLength(100).WithMessage("House name must not exceed 100 characters");

        RuleFor(x => x.Color)
            .MaximumLength(30).WithMessage("Color must not exceed 30 characters");
    }
}
