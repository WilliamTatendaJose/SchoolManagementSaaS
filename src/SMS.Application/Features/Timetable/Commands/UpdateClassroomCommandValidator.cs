using FluentValidation;

namespace SMS.Application.Features.Timetable.Commands;

public class UpdateClassroomCommandValidator : AbstractValidator<UpdateClassroomCommand>
{
    public UpdateClassroomCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Classroom id is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Classroom name is required")
            .MaximumLength(100).WithMessage("Classroom name must not exceed 100 characters");

        RuleFor(x => x.Capacity)
            .GreaterThanOrEqualTo(0).WithMessage("Capacity cannot be negative");
    }
}
