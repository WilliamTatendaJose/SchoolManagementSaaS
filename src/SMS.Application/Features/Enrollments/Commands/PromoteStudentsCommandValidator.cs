using FluentValidation;

namespace SMS.Application.Features.Enrollments.Commands;

public class PromoteStudentsCommandValidator : AbstractValidator<PromoteStudentsCommand>
{
    public PromoteStudentsCommandValidator()
    {
        RuleFor(x => x.ToClassId)
            .NotEmpty().WithMessage("Target class is required");

        RuleFor(x => x.ToAcademicYearId)
            .NotEmpty().WithMessage("Target academic year is required");

        RuleFor(x => x.StudentIds)
            .NotEmpty().WithMessage("At least one student is required");

        RuleForEach(x => x.StudentIds)
            .NotEmpty().WithMessage("Student id cannot be empty");
    }
}
