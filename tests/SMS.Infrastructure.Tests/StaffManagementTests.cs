using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Staffing;
using SMS.Application.Features.Staff.Commands;
using SMS.Domain.Entities;
using Xunit;
using StaffEntity = SMS.Domain.Entities.Staff;

namespace SMS.Infrastructure.Tests;

public class StaffManagementTests : IAsyncLifetime
{
    private readonly TenantTestContext _harness = new();
    private Guid _teacherStaffId;
    private Guid _subjectId;
    private Guid _spareUserId;

    public async Task InitializeAsync()
    {
        var tenant = new Tenant { Name = "Staff School", Code = "STF" };
        await using (var db = _harness.CreateDbContext())
        {
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
        }
        _harness.UseTenant(tenant.Id);

        await using (var db = _harness.CreateDbContext())
        {
            var teacherUser = new User { Email = "teacher@stf.zw", PasswordHash = "x", FirstName = "Tandi", LastName = "Moyo" };
            var spareUser = new User { Email = "spare@stf.zw", PasswordHash = "x", FirstName = "Spare", LastName = "User" };
            var subject = new Subject { Name = "Mathematics", Code = "MATH" };
            db.Users.AddRange(teacherUser, spareUser);
            db.Subjects.Add(subject);
            await db.SaveChangesAsync();

            var staff = new StaffEntity { UserId = teacherUser.Id, StaffNumber = "STAFF-2026-00001", IsTeacher = true, IsActive = true };
            db.Staff.Add(staff);
            await db.SaveChangesAsync();

            _teacherStaffId = staff.Id;
            _subjectId = subject.Id;
            _spareUserId = spareUser.Id;
            _harness.CurrentUser.UserId = teacherUser.Id; // approver context
        }
    }

    public Task DisposeAsync()
    {
        _harness.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Creating_staff_generates_a_number_and_rejects_duplicates()
    {
        await using var db = _harness.CreateDbContext();
        var handler = new CreateStaffCommandHandler(db);

        var first = await handler.Handle(new CreateStaffCommand { UserId = _spareUserId, IsTeacher = false }, CancellationToken.None);
        first.IsSuccess.Should().BeTrue();

        var created = await db.Staff.AsNoTracking().FirstAsync(s => s.Id == first.Data);
        created.StaffNumber.Should().StartWith("STAFF-");

        var duplicate = await handler.Handle(new CreateStaffCommand { UserId = _spareUserId }, CancellationToken.None);
        duplicate.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Assigning_a_subject_to_a_teacher_is_idempotent()
    {
        await using var db = _harness.CreateDbContext();
        var handler = new AssignSubjectToTeacherCommandHandler(db);

        var first = await handler.Handle(new AssignSubjectToTeacherCommand { StaffId = _teacherStaffId, SubjectId = _subjectId, IsPrimary = true }, CancellationToken.None);
        first.IsSuccess.Should().BeTrue();

        var second = await handler.Handle(new AssignSubjectToTeacherCommand { StaffId = _teacherStaffId, SubjectId = _subjectId }, CancellationToken.None);
        second.IsSuccess.Should().BeFalse("the subject is already assigned");

        (await db.TeacherSubjects.CountAsync(ts => ts.StaffId == _teacherStaffId)).Should().Be(1);
    }

    [Fact]
    public async Task Leave_request_can_be_approved_once()
    {
        Guid leaveId;
        await using (var db = _harness.CreateDbContext())
        {
            var create = await new CreateLeaveRequestCommandHandler(db).Handle(new CreateLeaveRequestCommand
            {
                StaffId = _teacherStaffId,
                LeaveType = "Sick",
                StartDate = new DateTime(2026, 3, 1),
                EndDate = new DateTime(2026, 3, 3)
            }, CancellationToken.None);
            create.IsSuccess.Should().BeTrue();
            leaveId = create.Data;
        }

        await using (var db = _harness.CreateDbContext())
        {
            var approve = await new ReviewLeaveRequestCommandHandler(db, _harness.CurrentUser)
                .Handle(new ReviewLeaveRequestCommand { LeaveRequestId = leaveId, Approve = true, Comments = "Get well" }, CancellationToken.None);
            approve.IsSuccess.Should().BeTrue();
        }

        await using (var db = _harness.CreateDbContext())
        {
            var leave = await db.LeaveRequests.AsNoTracking().FirstAsync(l => l.Id == leaveId);
            leave.Status.Should().Be(LeaveStatuses.Approved);
            leave.ApprovedById.Should().Be(_teacherStaffId, "the approver's staff record is recorded");
            leave.ApprovedAt.Should().NotBeNull();
        }

        await using (var db = _harness.CreateDbContext())
        {
            var again = await new ReviewLeaveRequestCommandHandler(db, _harness.CurrentUser)
                .Handle(new ReviewLeaveRequestCommand { LeaveRequestId = leaveId, Approve = false }, CancellationToken.None);
            again.IsSuccess.Should().BeFalse("an already-approved request cannot be reviewed again");
        }
    }
}
