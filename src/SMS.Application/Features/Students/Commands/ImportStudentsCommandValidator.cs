using FluentValidation;

namespace SMS.Application.Features.Students.Commands;

public class ImportStudentsCommandValidator : AbstractValidator<ImportStudentsCommand>
{
    private const int MaxRows = 2000;

    public ImportStudentsCommandValidator()
    {
        RuleFor(x => x.Rows)
            .NotEmpty().WithMessage("No rows to import")
            .Must(rows => rows.Count <= MaxRows).WithMessage($"Cannot import more than {MaxRows} rows at once - split the file into batches");
    }
}
