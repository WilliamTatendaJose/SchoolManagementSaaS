using FluentValidation;

namespace SMS.Application.Features.Boarding.Commands;

public class RequestWeekendLeaveCommandValidator : AbstractValidator<RequestWeekendLeaveCommand>
{
    public RequestWeekendLeaveCommandValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("Student is required");

        RuleFor(x => x.DepartureDate)
            .NotEmpty().WithMessage("Departure date is required");

        RuleFor(x => x.ExpectedReturnDate)
            .NotEmpty().WithMessage("Expected return date is required")
            .GreaterThanOrEqualTo(x => x.DepartureDate)
            .WithMessage("Expected return cannot be before departure");

        RuleFor(x => x.Destination)
            .NotEmpty().WithMessage("Destination is required")
            .MaximumLength(200);

        RuleFor(x => x.Reason)
            .MaximumLength(500);
    }
}
