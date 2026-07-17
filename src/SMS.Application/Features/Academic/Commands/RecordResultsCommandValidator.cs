using FluentValidation;

namespace SMS.Application.Features.Academic.Commands;

public class RecordResultsCommandValidator : AbstractValidator<RecordResultsCommand>
{
    public RecordResultsCommandValidator()
    {
        RuleFor(x => x.AssessmentId)
            .NotEmpty().WithMessage("Assessment is required");

        RuleFor(x => x.Results)
            .NotEmpty().WithMessage("At least one result is required");

        RuleForEach(x => x.Results).ChildRules(result =>
        {
            result.RuleFor(r => r.StudentId)
                .NotEmpty().WithMessage("Student is required");

            result.RuleFor(r => r.Score)
                .GreaterThanOrEqualTo(0).WithMessage("Score cannot be negative");
        });
    }
}
