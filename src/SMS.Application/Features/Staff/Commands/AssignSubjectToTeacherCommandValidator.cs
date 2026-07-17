using FluentValidation;

namespace SMS.Application.Features.Staff.Commands;

public class AssignSubjectToTeacherCommandValidator : AbstractValidator<AssignSubjectToTeacherCommand>
{
    public AssignSubjectToTeacherCommandValidator()
    {
        RuleFor(x => x.StaffId)
            .NotEmpty().WithMessage("Staff is required");

        RuleFor(x => x.SubjectId)
            .NotEmpty().WithMessage("Subject is required");
    }
}
