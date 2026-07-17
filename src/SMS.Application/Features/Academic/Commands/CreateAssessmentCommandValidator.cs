using FluentValidation;

namespace SMS.Application.Features.Academic.Commands;

public class CreateAssessmentCommandValidator : AbstractValidator<CreateAssessmentCommand>
{
    public CreateAssessmentCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Assessment name is required")
            .MaximumLength(150).WithMessage("Assessment name must not exceed 150 characters");

        RuleFor(x => x.SubjectId)
            .NotEmpty().WithMessage("Subject is required");

        RuleFor(x => x.ClassId)
            .NotEmpty().WithMessage("Class is required");

        RuleFor(x => x.AcademicTermId)
            .NotEmpty().WithMessage("Academic term is required");

        RuleFor(x => x.AssessmentType)
            .NotEmpty().WithMessage("Assessment type is required");

        RuleFor(x => x.MaxScore)
            .GreaterThan(0).WithMessage("Max score must be greater than zero");

        RuleFor(x => x.WeightPercentage)
            .InclusiveBetween(0, 100).WithMessage("Weight percentage must be between 0 and 100");
    }
}
