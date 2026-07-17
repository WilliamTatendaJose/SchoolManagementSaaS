using FluentValidation.TestHelper;
using SMS.Application.Features.Boarding.Commands;
using Xunit;

namespace SMS.Application.Tests.Features.Boarding;

public class RequestWeekendLeaveCommandValidatorTests
{
    private readonly RequestWeekendLeaveCommandValidator _validator = new();

    private static RequestWeekendLeaveCommand ValidCommand() => new()
    {
        StudentId = Guid.NewGuid(),
        DepartureDate = new DateTime(2026, 3, 7),
        ExpectedReturnDate = new DateTime(2026, 3, 8),
        Destination = "Home"
    };

    [Fact]
    public void Validate_ShouldPass_WhenValid()
    {
        _validator.TestValidate(ValidCommand()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldFail_WhenStudentIdEmpty()
    {
        _validator.TestValidate(ValidCommand() with { StudentId = Guid.Empty })
            .ShouldHaveValidationErrorFor(x => x.StudentId);
    }

    [Fact]
    public void Validate_ShouldFail_WhenDestinationEmpty()
    {
        _validator.TestValidate(ValidCommand() with { Destination = "" })
            .ShouldHaveValidationErrorFor(x => x.Destination);
    }

    [Fact]
    public void Validate_ShouldFail_WhenReturnBeforeDeparture()
    {
        _validator.TestValidate(ValidCommand() with { ExpectedReturnDate = new DateTime(2026, 3, 6) })
            .ShouldHaveValidationErrorFor(x => x.ExpectedReturnDate);
    }
}
