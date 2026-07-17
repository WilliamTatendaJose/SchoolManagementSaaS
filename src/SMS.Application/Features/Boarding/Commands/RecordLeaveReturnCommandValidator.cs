using FluentValidation;

namespace SMS.Application.Features.Boarding.Commands;

public class RecordLeaveReturnCommandValidator : AbstractValidator<RecordLeaveReturnCommand>
{
    public RecordLeaveReturnCommandValidator()
    {
        RuleFor(x => x.LeaveId)
            .NotEmpty().WithMessage("Leave request is required");
    }
}
