using FluentValidation;

namespace SMS.Application.Features.Enrollments.Commands;

public class TransferStudentCommandValidator : AbstractValidator<TransferStudentCommand>
{
    public TransferStudentCommandValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("Student is required");

        RuleFor(x => x.AcademicYearId)
            .NotEmpty().WithMessage("Academic year is required");

        RuleFor(x => x.ToClassId)
            .NotEmpty().WithMessage("Target class is required");
    }
}
