using FluentValidation.TestHelper;
using SMS.Application.Features.Staff.Commands;
using Xunit;

namespace SMS.Application.Tests.Features.Staff;

public class StaffValidatorTests
{
    private readonly CreateStaffCommandValidator _createValidator = new();
    private readonly CreateLeaveRequestCommandValidator _leaveValidator = new();

    [Fact]
    public void CreateStaff_ShouldPass_WhenUserProvided()
    {
        _createValidator.TestValidate(new CreateStaffCommand { UserId = Guid.NewGuid(), IsTeacher = true })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreateStaff_ShouldFail_WhenUserIdEmpty()
    {
        _createValidator.TestValidate(new CreateStaffCommand { UserId = Guid.Empty })
            .ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void CreateStaff_ShouldFail_WhenDateOfJoiningInFuture()
    {
        _createValidator.TestValidate(new CreateStaffCommand { UserId = Guid.NewGuid(), DateOfJoining = DateTime.UtcNow.AddMonths(2) })
            .ShouldHaveValidationErrorFor(x => x.DateOfJoining);
    }

    [Fact]
    public void CreateLeave_ShouldPass_WhenValid()
    {
        var command = new CreateLeaveRequestCommand
        {
            StaffId = Guid.NewGuid(),
            LeaveType = "Sick",
            StartDate = new DateTime(2026, 3, 1),
            EndDate = new DateTime(2026, 3, 3)
        };
        _leaveValidator.TestValidate(command).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreateLeave_ShouldFail_WhenEndBeforeStart()
    {
        var command = new CreateLeaveRequestCommand
        {
            StaffId = Guid.NewGuid(),
            LeaveType = "Sick",
            StartDate = new DateTime(2026, 3, 5),
            EndDate = new DateTime(2026, 3, 1)
        };
        _leaveValidator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.EndDate);
    }

    [Fact]
    public void CreateLeave_ShouldFail_WhenLeaveTypeEmpty()
    {
        var command = new CreateLeaveRequestCommand
        {
            StaffId = Guid.NewGuid(),
            LeaveType = "",
            StartDate = new DateTime(2026, 3, 1),
            EndDate = new DateTime(2026, 3, 3)
        };
        _leaveValidator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.LeaveType);
    }
}
