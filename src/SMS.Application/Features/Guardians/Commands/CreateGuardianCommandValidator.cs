using FluentValidation;

namespace SMS.Application.Features.Guardians.Commands;

public class CreateGuardianCommandValidator : AbstractValidator<CreateGuardianCommand>
{
    public CreateGuardianCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters");

        RuleFor(x => x.Gender)
            .NotEmpty().WithMessage("Gender is required")
            .Must(g => g == "Male" || g == "Female" || g == "Other")
            .WithMessage("Gender must be Male, Female, or Other");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("A valid email address is required");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required")
            .MaximumLength(30).WithMessage("Phone number must not exceed 30 characters");

        When(x => x.LinkToStudent is not null, () =>
        {
            RuleFor(x => x.LinkToStudent!.StudentId)
                .NotEmpty().WithMessage("Student is required when linking a guardian");

            RuleFor(x => x.LinkToStudent!.Relationship)
                .NotEmpty().WithMessage("Relationship is required when linking to a student")
                .MaximumLength(50).WithMessage("Relationship must not exceed 50 characters");
        });
    }
}
