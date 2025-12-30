using FluentValidation;

namespace SMS.Application.Features.Students.Commands;

public class CreateStudentCommandValidator : AbstractValidator<CreateStudentCommand>
{
    public CreateStudentCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required")
            .LessThan(DateTime.Today).WithMessage("Date of birth must be in the past")
            .GreaterThan(DateTime.Today.AddYears(-25)).WithMessage("Student cannot be older than 25 years");

        RuleFor(x => x.Gender)
            .NotEmpty().WithMessage("Gender is required")
            .Must(g => g == "Male" || g == "Female" || g == "Other")
            .WithMessage("Gender must be Male, Female, or Other");

        RuleFor(x => x.AdmissionDate)
            .NotEmpty().WithMessage("Admission date is required");

        RuleForEach(x => x.Guardians).ChildRules(guardian =>
        {
            guardian.RuleFor(g => g.Relationship)
                .NotEmpty().WithMessage("Guardian relationship is required");

            guardian.RuleFor(g => g.Phone)
                .NotEmpty().When(g => g.IsPrimaryContact)
                .WithMessage("Phone number is required for primary contact");
        });
    }
}
