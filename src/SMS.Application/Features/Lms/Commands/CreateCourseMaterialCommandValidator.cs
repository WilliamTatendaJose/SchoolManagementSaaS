using FluentValidation;

namespace SMS.Application.Features.Lms.Commands;

public class CreateCourseMaterialCommandValidator : AbstractValidator<CreateCourseMaterialCommand>
{
    public CreateCourseMaterialCommandValidator()
    {
        RuleFor(x => x.ClassId)
            .NotEmpty().WithMessage("Class is required");

        RuleFor(x => x.SubjectId)
            .NotEmpty().WithMessage("Subject is required");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(300);

        RuleFor(x => x.Description)
            .MaximumLength(2000);

        RuleFor(x => x.Url)
            .MaximumLength(2000)
            .Must(BeAWellFormedUrl).When(x => !string.IsNullOrWhiteSpace(x.Url))
            .WithMessage("Link must be a valid URL");

        // A material carries content one way or the other: an external link or an upload.
        RuleFor(x => x)
            .Must(c => !string.IsNullOrWhiteSpace(c.Url) || (c.AttachmentContent is { Length: > 0 }))
            .WithMessage("Provide a link or attach a file");
    }

    private static bool BeAWellFormedUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
