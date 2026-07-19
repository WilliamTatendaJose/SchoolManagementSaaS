using FluentValidation;

namespace SMS.Application.Features.Lms.Commands;

public class BulkGradeAssignmentSubmissionsCommandValidator : AbstractValidator<BulkGradeAssignmentSubmissionsCommand>
{
    public BulkGradeAssignmentSubmissionsCommandValidator()
    {
        RuleFor(x => x.AssignmentId)
            .NotEmpty().WithMessage("Assignment is required");

        RuleForEach(x => x.Grades).ChildRules(entry =>
        {
            entry.RuleFor(g => g.StudentId)
                .NotEmpty().WithMessage("Student is required");

            entry.RuleFor(g => g.Grade)
                .GreaterThanOrEqualTo(0).WithMessage("Grade cannot be negative")
                .When(g => g.Grade.HasValue);

            entry.RuleFor(g => g.Feedback)
                .MaximumLength(2000);
        });
    }
}
