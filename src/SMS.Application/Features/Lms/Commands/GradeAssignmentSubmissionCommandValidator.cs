using FluentValidation;

namespace SMS.Application.Features.Lms.Commands;

public class GradeAssignmentSubmissionCommandValidator : AbstractValidator<GradeAssignmentSubmissionCommand>
{
    public GradeAssignmentSubmissionCommandValidator()
    {
        RuleFor(x => x.SubmissionId)
            .NotEmpty().WithMessage("Submission is required");

        RuleFor(x => x.Grade)
            .GreaterThanOrEqualTo(0).WithMessage("Grade cannot be negative");

        RuleFor(x => x.Feedback)
            .MaximumLength(2000);
    }
}
