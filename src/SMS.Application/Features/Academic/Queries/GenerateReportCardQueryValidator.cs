using FluentValidation;

namespace SMS.Application.Features.Academic.Queries;

public class GenerateReportCardQueryValidator : AbstractValidator<GenerateReportCardQuery>
{
    public GenerateReportCardQueryValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("Student is required");

        RuleFor(x => x.AcademicTermId)
            .NotEmpty().WithMessage("Academic term is required");
    }
}
