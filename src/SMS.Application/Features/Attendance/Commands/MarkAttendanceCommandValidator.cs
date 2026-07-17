using FluentValidation;

namespace SMS.Application.Features.Attendance.Commands;

public class MarkAttendanceCommandValidator : AbstractValidator<MarkAttendanceCommand>
{
    private static readonly string[] ValidStatuses = ["Present", "Absent", "Late", "Excused"];

    public MarkAttendanceCommandValidator()
    {
        RuleFor(x => x.ClassId)
            .NotEmpty().WithMessage("Class is required");

        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Date is required")
            .LessThanOrEqualTo(_ => DateTime.UtcNow.Date.AddDays(1)).WithMessage("Attendance date cannot be in the future");

        RuleFor(x => x.Records)
            .NotEmpty().WithMessage("At least one attendance record is required");

        RuleForEach(x => x.Records).ChildRules(record =>
        {
            record.RuleFor(r => r.StudentId)
                .NotEmpty().WithMessage("Student is required");

            record.RuleFor(r => r.Status)
                .NotEmpty().WithMessage("Attendance status is required")
                .Must(s => ValidStatuses.Contains(s))
                .WithMessage("Status must be Present, Absent, Late, or Excused");
        });
    }
}
