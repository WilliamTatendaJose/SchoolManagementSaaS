using FluentValidation;

namespace SMS.Application.Features.Fees.Commands;

public class UpdateFeeStructureCommandValidator : AbstractValidator<UpdateFeeStructureCommand>
{
    public UpdateFeeStructureCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Fee structure id is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(150).WithMessage("Name must not exceed 150 characters");

        RuleFor(x => x.FeeType)
            .NotEmpty().WithMessage("Fee type is required")
            .MaximumLength(50).WithMessage("Fee type must not exceed 50 characters");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero");
    }
}
