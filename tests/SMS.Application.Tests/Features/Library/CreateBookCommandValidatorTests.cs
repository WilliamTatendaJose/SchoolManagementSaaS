using FluentValidation.TestHelper;
using SMS.Application.Features.Library.Commands;
using Xunit;

namespace SMS.Application.Tests.Features.Library;

public class CreateBookCommandValidatorTests
{
    private readonly CreateBookCommandValidator _validator = new();

    private static CreateBookCommand ValidCommand() => new()
    {
        Title = "Things Fall Apart",
        Author = "Chinua Achebe",
        TotalCopies = 3
    };

    [Fact]
    public void Validate_ShouldPass_WhenValid()
    {
        _validator.TestValidate(ValidCommand()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldFail_WhenTitleEmpty()
    {
        _validator.TestValidate(ValidCommand() with { Title = "" })
            .ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Validate_ShouldFail_WhenAuthorEmpty()
    {
        _validator.TestValidate(ValidCommand() with { Author = "" })
            .ShouldHaveValidationErrorFor(x => x.Author);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ShouldFail_WhenTotalCopiesNotPositive(int copies)
    {
        _validator.TestValidate(ValidCommand() with { TotalCopies = copies })
            .ShouldHaveValidationErrorFor(x => x.TotalCopies);
    }
}
