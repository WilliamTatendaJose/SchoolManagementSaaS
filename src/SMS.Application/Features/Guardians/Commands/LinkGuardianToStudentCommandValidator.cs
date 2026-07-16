using FluentValidation;

namespace SMS.Application.Features.Guardians.Commands;

public class LinkGuardianToStudentCommandValidator : AbstractValidator<LinkGuardianToStudentCommand>
{
    public LinkGuardianToStudentCommandValidator()
    {
        RuleFor(x => x.GuardianId)
            .NotEmpty().WithMessage("Guardian is required");

        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("Student is required");

        RuleFor(x => x.Relationship)
            .NotEmpty().WithMessage("Relationship is required")
            .MaximumLength(50).WithMessage("Relationship must not exceed 50 characters");
    }
}
