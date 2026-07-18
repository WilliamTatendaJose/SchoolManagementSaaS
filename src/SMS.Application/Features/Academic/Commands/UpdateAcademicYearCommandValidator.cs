using FluentValidation;

namespace SMS.Application.Features.Academic.Commands;

public class UpdateAcademicYearCommandValidator : AbstractValidator<UpdateAcademicYearCommand>
{
    public UpdateAcademicYearCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Academic year id is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Academic year name is required")
            .MaximumLength(50).WithMessage("Name must not exceed 50 characters");

        RuleFor(x => x.Year)
            .InclusiveBetween(2000, 2100).WithMessage("Year must be a realistic value");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate).WithMessage("End date must be after the start date");
    }
}
