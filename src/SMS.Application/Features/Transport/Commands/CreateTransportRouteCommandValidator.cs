using FluentValidation;

namespace SMS.Application.Features.Transport.Commands;

public class CreateTransportRouteCommandValidator : AbstractValidator<CreateTransportRouteCommand>
{
    public CreateTransportRouteCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Route name is required")
            .MaximumLength(200);

        RuleFor(x => x.VehicleRegistration)
            .MaximumLength(20);

        RuleFor(x => x.Capacity)
            .GreaterThan(0).WithMessage("Capacity must be greater than zero");
    }
}
