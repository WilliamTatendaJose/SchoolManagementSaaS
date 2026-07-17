using FluentValidation;

namespace SMS.Application.Features.Lms.Commands;

public class RecordAssignmentSubmissionCommandValidator : AbstractValidator<RecordAssignmentSubmissionCommand>
{
    public RecordAssignmentSubmissionCommandValidator()
    {
        RuleFor(x => x.AssignmentId)
            .NotEmpty().WithMessage("Assignment is required");

        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("Student is required");

        RuleFor(x => x.Comment)
            .MaximumLength(2000);
    }
}
