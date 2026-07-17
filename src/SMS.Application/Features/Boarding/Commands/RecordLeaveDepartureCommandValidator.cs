using FluentValidation;

namespace SMS.Application.Features.Boarding.Commands;

public class RecordLeaveDepartureCommandValidator : AbstractValidator<RecordLeaveDepartureCommand>
{
    public RecordLeaveDepartureCommandValidator()
    {
        RuleFor(x => x.LeaveId)
            .NotEmpty().WithMessage("Leave request is required");

        RuleFor(x => x.CollectedBy)
            .NotEmpty().WithMessage("Name of the person collecting the student is required")
            .MaximumLength(200);
    }
}
