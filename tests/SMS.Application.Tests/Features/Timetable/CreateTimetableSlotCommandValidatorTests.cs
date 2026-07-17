using FluentValidation.TestHelper;
using SMS.Application.Features.Timetable.Commands;
using Xunit;

namespace SMS.Application.Tests.Features.Timetable;

public class CreateTimetableSlotCommandValidatorTests
{
    private readonly CreateTimetableSlotCommandValidator _validator = new();

    private static CreateTimetableSlotCommand ValidCommand() => new()
    {
        ClassId = Guid.NewGuid(),
        SubjectId = Guid.NewGuid(),
        TeacherId = Guid.NewGuid(),
        AcademicTermId = Guid.NewGuid(),
        DayOfWeek = DayOfWeek.Monday,
        StartTime = new TimeOnly(8, 0),
        EndTime = new TimeOnly(9, 0)
    };

    [Fact]
    public void Validate_ShouldPass_WhenValid()
    {
        _validator.TestValidate(ValidCommand()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldFail_WhenEndBeforeStart()
    {
        _validator.TestValidate(ValidCommand() with { StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(8, 0) })
            .ShouldHaveValidationErrorFor(x => x.EndTime);
    }

    [Fact]
    public void Validate_ShouldFail_WhenTeacherMissing()
    {
        _validator.TestValidate(ValidCommand() with { TeacherId = Guid.Empty })
            .ShouldHaveValidationErrorFor(x => x.TeacherId);
    }
}
