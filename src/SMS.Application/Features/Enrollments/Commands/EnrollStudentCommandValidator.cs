using FluentValidation;

namespace SMS.Application.Features.Enrollments.Commands;

public class EnrollStudentCommandValidator : AbstractValidator<EnrollStudentCommand>
{
    public EnrollStudentCommandValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("Student is required");

        RuleFor(x => x.ClassId)
            .NotEmpty().WithMessage("Class is required");

        RuleFor(x => x.AcademicYearId)
            .NotEmpty().WithMessage("Academic year is required");

        RuleFor(x => x.EnrollmentDate)
            .Must(date => !date.HasValue || date.Value <= DateTime.UtcNow.AddDays(1))
            .WithMessage("Enrollment date cannot be in the future");
    }
}
