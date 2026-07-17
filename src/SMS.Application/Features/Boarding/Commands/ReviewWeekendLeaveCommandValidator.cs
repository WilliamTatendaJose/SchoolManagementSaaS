using FluentValidation;

namespace SMS.Application.Features.Boarding.Commands;

public class ReviewWeekendLeaveCommandValidator : AbstractValidator<ReviewWeekendLeaveCommand>
{
    public ReviewWeekendLeaveCommandValidator()
    {
        RuleFor(x => x.LeaveId)
            .NotEmpty().WithMessage("Leave request is required");

        RuleFor(x => x.ReviewNote)
            .MaximumLength(500);
    }
}
