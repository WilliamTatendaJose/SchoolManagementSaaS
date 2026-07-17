using FluentValidation;

namespace SMS.Application.Features.Academic.Commands;

public class SaveReportCardCommentCommandValidator : AbstractValidator<SaveReportCardCommentCommand>
{
    public SaveReportCardCommentCommandValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("Student is required");

        RuleFor(x => x.AcademicTermId)
            .NotEmpty().WithMessage("Academic term is required");

        RuleFor(x => x.ClassTeacherComment)
            .MaximumLength(1000);

        RuleFor(x => x.HeadComment)
            .MaximumLength(1000);

        RuleFor(x => x)
            .Must(x => x.ClassTeacherComment != null || x.HeadComment != null)
            .WithMessage("Provide at least one of ClassTeacherComment or HeadComment");
    }
}
