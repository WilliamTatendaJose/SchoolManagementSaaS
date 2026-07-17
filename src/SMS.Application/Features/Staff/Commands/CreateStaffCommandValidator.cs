using FluentValidation;

namespace SMS.Application.Features.Staff.Commands;

public class CreateStaffCommandValidator : AbstractValidator<CreateStaffCommand>
{
    public CreateStaffCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User is required");

        RuleFor(x => x.Department)
            .MaximumLength(100).WithMessage("Department must not exceed 100 characters");

        RuleFor(x => x.JobTitle)
            .MaximumLength(100).WithMessage("Job title must not exceed 100 characters");

        RuleFor(x => x.DateOfJoining)
            .LessThanOrEqualTo(_ => DateTime.UtcNow.Date.AddDays(1))
            .When(x => x.DateOfJoining.HasValue)
            .WithMessage("Date of joining cannot be in the future");
    }
}
