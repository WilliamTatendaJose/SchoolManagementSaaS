using FluentValidation;

namespace SMS.Application.Features.Discipline.Commands;

public class CreateDisciplineRecordCommandValidator : AbstractValidator<CreateDisciplineRecordCommand>
{
    public CreateDisciplineRecordCommandValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("Student is required");

        RuleFor(x => x.IncidentDate)
            .NotEmpty().WithMessage("Incident date is required")
            .LessThanOrEqualTo(_ => DateTime.UtcNow.Date.AddDays(1)).WithMessage("Incident date cannot be in the future");

        RuleFor(x => x.IncidentType)
            .NotEmpty().WithMessage("Incident type is required")
            .MaximumLength(100).WithMessage("Incident type must not exceed 100 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters");

        RuleFor(x => x.DemeritsAwarded)
            .GreaterThanOrEqualTo(0).When(x => x.DemeritsAwarded.HasValue)
            .WithMessage("Demerits cannot be negative");

        RuleFor(x => x.MeritsAwarded)
            .GreaterThanOrEqualTo(0).When(x => x.MeritsAwarded.HasValue)
            .WithMessage("Merits cannot be negative");

        RuleFor(x => x.Channel)
            .NotEmpty().When(x => x.NotifyGuardian).WithMessage("Channel is required to notify the guardian");
    }
}
