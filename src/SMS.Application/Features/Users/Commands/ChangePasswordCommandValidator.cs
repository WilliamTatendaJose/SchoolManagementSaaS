using FluentValidation;

namespace SMS.Application.Features.Users.Commands;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User is required");

        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches("[A-Za-z]").WithMessage("Password must contain a letter")
            .Matches("[0-9]").WithMessage("Password must contain a digit")
            .NotEqual(x => x.CurrentPassword).WithMessage("New password must differ from the current password");
    }
}
