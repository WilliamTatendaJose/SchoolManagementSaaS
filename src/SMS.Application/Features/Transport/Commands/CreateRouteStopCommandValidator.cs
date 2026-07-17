using FluentValidation;

namespace SMS.Application.Features.Transport.Commands;

public class CreateRouteStopCommandValidator : AbstractValidator<CreateRouteStopCommand>
{
    public CreateRouteStopCommandValidator()
    {
        RuleFor(x => x.TransportRouteId)
            .NotEmpty().WithMessage("Transport route is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Stop name is required")
            .MaximumLength(200);

        RuleFor(x => x.SequenceNumber)
            .GreaterThanOrEqualTo(0);
    }
}
