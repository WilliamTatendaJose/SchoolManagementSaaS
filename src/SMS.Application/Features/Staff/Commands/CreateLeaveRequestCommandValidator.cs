using FluentValidation;

namespace SMS.Application.Features.Staff.Commands;

public class CreateLeaveRequestCommandValidator : AbstractValidator<CreateLeaveRequestCommand>
{
    public CreateLeaveRequestCommandValidator()
    {
        RuleFor(x => x.StaffId)
            .NotEmpty().WithMessage("Staff is required");

        RuleFor(x => x.LeaveType)
            .NotEmpty().WithMessage("Leave type is required")
            .MaximumLength(50).WithMessage("Leave type must not exceed 50 characters");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("End date is required")
            .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("End date cannot be before the start date");
    }
}
