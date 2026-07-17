using FluentValidation;

namespace SMS.Application.Features.Timetable.Commands;

public class UpdateTimetableSlotCommandValidator : AbstractValidator<UpdateTimetableSlotCommand>
{
    public UpdateTimetableSlotCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Timetable slot id is required");
        RuleFor(x => x.ClassId).NotEmpty().WithMessage("Class is required");
        RuleFor(x => x.SubjectId).NotEmpty().WithMessage("Subject is required");
        RuleFor(x => x.TeacherId).NotEmpty().WithMessage("Teacher is required");
        RuleFor(x => x.AcademicTermId).NotEmpty().WithMessage("Academic term is required");

        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.StartTime).WithMessage("End time must be after the start time");
    }
}
