using FluentValidation;

namespace SMS.Application.Features.Users.Commands;

public class AssignRolesCommandValidator : AbstractValidator<AssignRolesCommand>
{
    public AssignRolesCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User is required");

        RuleFor(x => x.Roles)
            .NotEmpty().WithMessage("At least one role is required");

        RuleForEach(x => x.Roles)
            .NotEmpty().WithMessage("Role name cannot be empty");
    }
}
