using FluentValidation;

namespace SMS.Application.Features.Academic.Commands;

public class UpdateSubjectCommandValidator : AbstractValidator<UpdateSubjectCommand>
{
    public UpdateSubjectCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Subject id is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Subject name is required")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Subject code is required")
            .MaximumLength(20).WithMessage("Code must not exceed 20 characters");
    }
}
