using FluentValidation.TestHelper;
using SMS.Application.Features.Attendance.Commands;
using Xunit;

namespace SMS.Application.Tests.Features.Attendance;

public class MarkAttendanceCommandValidatorTests
{
    private readonly MarkAttendanceCommandValidator _validator = new();

    private static MarkAttendanceCommand ValidCommand() => new()
    {
        ClassId = Guid.NewGuid(),
        Date = DateTime.UtcNow.Date,
        Records = [new AttendanceRecordDto { StudentId = Guid.NewGuid(), Status = "Present" }]
    };

    [Fact]
    public void Validate_ShouldPass_WhenValid()
    {
        _validator.TestValidate(ValidCommand()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldFail_WhenNoRecords()
    {
        _validator.TestValidate(ValidCommand() with { Records = [] })
            .ShouldHaveValidationErrorFor(x => x.Records);
    }

    [Fact]
    public void Validate_ShouldFail_WhenDateInFuture()
    {
        _validator.TestValidate(ValidCommand() with { Date = DateTime.UtcNow.Date.AddDays(5) })
            .ShouldHaveValidationErrorFor(x => x.Date);
    }

    [Fact]
    public void Validate_ShouldFail_WhenRecordStatusInvalid()
    {
        var command = ValidCommand() with
        {
            Records = [new AttendanceRecordDto { StudentId = Guid.NewGuid(), Status = "Maybe" }]
        };
        _validator.TestValidate(command).ShouldHaveValidationErrorFor("Records[0].Status");
    }
}
