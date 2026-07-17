using FluentValidation;

namespace SMS.Application.Features.Academic.Commands;

public class CreateAcademicYearCommandValidator : AbstractValidator<CreateAcademicYearCommand>
{
    public CreateAcademicYearCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Academic year name is required")
            .MaximumLength(50).WithMessage("Name must not exceed 50 characters");

        RuleFor(x => x.Year)
            .InclusiveBetween(2000, 2100).WithMessage("Year must be a realistic value");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate).WithMessage("End date must be after the start date");

        RuleForEach(x => x.Terms).ChildRules(term =>
        {
            term.RuleFor(t => t.Name)
                .NotEmpty().WithMessage("Term name is required");

            term.RuleFor(t => t.TermNumber)
                .GreaterThan(0).WithMessage("Term number must be greater than zero");

            term.RuleFor(t => t.EndDate)
                .GreaterThan(t => t.StartDate).WithMessage("Term end date must be after its start date");
        });
    }
}
