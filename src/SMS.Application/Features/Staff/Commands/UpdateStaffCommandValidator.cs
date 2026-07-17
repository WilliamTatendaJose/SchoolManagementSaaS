using FluentValidation;

namespace SMS.Application.Features.Staff.Commands;

public class UpdateStaffCommandValidator : AbstractValidator<UpdateStaffCommand>
{
    public UpdateStaffCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Staff id is required");

        RuleFor(x => x.Department)
            .MaximumLength(100).WithMessage("Department must not exceed 100 characters");

        RuleFor(x => x.JobTitle)
            .MaximumLength(100).WithMessage("Job title must not exceed 100 characters");
    }
}
