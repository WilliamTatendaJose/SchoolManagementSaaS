using FluentValidation;

namespace SMS.Application.Features.Timetable.Commands;

public class CreateClassroomCommandValidator : AbstractValidator<CreateClassroomCommand>
{
    public CreateClassroomCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Classroom name is required")
            .MaximumLength(100).WithMessage("Classroom name must not exceed 100 characters");

        RuleFor(x => x.Building)
            .MaximumLength(100).WithMessage("Building must not exceed 100 characters");

        RuleFor(x => x.Capacity)
            .GreaterThanOrEqualTo(0).WithMessage("Capacity cannot be negative");
    }
}
