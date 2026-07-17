using FluentValidation.TestHelper;
using SMS.Application.Features.Discipline.Commands;
using Xunit;

namespace SMS.Application.Tests.Features.Discipline;

public class CreateDisciplineRecordCommandValidatorTests
{
    private readonly CreateDisciplineRecordCommandValidator _validator = new();

    private static CreateDisciplineRecordCommand ValidCommand() => new()
    {
        StudentId = Guid.NewGuid(),
        IncidentDate = DateTime.UtcNow.Date,
        IncidentType = "Late",
        Description = "Arrived late."
    };

    [Fact]
    public void Validate_ShouldPass_WhenValid()
    {
        _validator.TestValidate(ValidCommand()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldFail_WhenStudentEmpty()
    {
        _validator.TestValidate(ValidCommand() with { StudentId = Guid.Empty })
            .ShouldHaveValidationErrorFor(x => x.StudentId);
    }

    [Fact]
    public void Validate_ShouldFail_WhenIncidentTypeEmpty()
    {
        _validator.TestValidate(ValidCommand() with { IncidentType = "" })
            .ShouldHaveValidationErrorFor(x => x.IncidentType);
    }

    [Fact]
    public void Validate_ShouldFail_WhenIncidentDateInFuture()
    {
        _validator.TestValidate(ValidCommand() with { IncidentDate = DateTime.UtcNow.AddDays(10) })
            .ShouldHaveValidationErrorFor(x => x.IncidentDate);
    }

    [Fact]
    public void Validate_ShouldFail_WhenDemeritsNegative()
    {
        _validator.TestValidate(ValidCommand() with { DemeritsAwarded = -1 })
            .ShouldHaveValidationErrorFor(x => x.DemeritsAwarded);
    }
}
