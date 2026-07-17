using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Messaging;
using SMS.Application.Features.Boarding;
using SMS.Application.Features.Boarding.Commands;
using SMS.Application.Features.Boarding.Queries;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using Xunit;

namespace SMS.Infrastructure.Tests;

public class WeekendLeaveTests : IAsyncLifetime
{
    private readonly TenantTestContext _harness = new();
    private Guid _boarderId;
    private Guid _dayScholarId;
    private const string GuardianPhone = "+263772111222";

    public async Task InitializeAsync()
    {
        var tenant = new Tenant { Name = "Boarding School", Code = "BRD" };
        await using (var db = _harness.CreateDbContext())
        {
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
        }
        _harness.UseTenant(tenant.Id);

        await using (var db = _harness.CreateDbContext())
        {
            var user = new User { Email = "matron@brd.zw", PasswordHash = "x", FirstName = "Mat", LastName = "Ron" };
            db.Users.Add(user);

            var dorm = new Dormitory { Name = "East Wing", Capacity = 30, Gender = "Male" };
            db.Dormitories.Add(dorm);
            await db.SaveChangesAsync();

            var boarder = new Student { StudentNumber = "S-BRD-1", FirstName = "Bo", LastName = "Arder", DateOfBirth = new DateTime(2011, 1, 1), Gender = Gender.Male, AdmissionDate = new DateTime(2026, 1, 1), Status = StudentStatus.Active, DormitoryId = dorm.Id };
            var dayScholar = new Student { StudentNumber = "S-BRD-2", FirstName = "Day", LastName = "Scholar", DateOfBirth = new DateTime(2011, 5, 1), Gender = Gender.Female, AdmissionDate = new DateTime(2026, 1, 1), Status = StudentStatus.Active };
            db.Students.Add(boarder);
            db.Students.Add(dayScholar);

            var guardian = new Guardian { FirstName = "Gee", LastName = "Parent", Gender = Gender.Other, Phone = GuardianPhone };
            db.Guardians.Add(guardian);
            await db.SaveChangesAsync();

            db.StudentGuardians.Add(new StudentGuardian { StudentId = boarder.Id, GuardianId = guardian.Id, Relationship = "Parent", IsPrimaryContact = true });
            await db.SaveChangesAsync();

            _boarderId = boarder.Id;
            _dayScholarId = dayScholar.Id;
            _harness.CurrentUser.UserId = user.Id;
        }
    }

    public Task DisposeAsync()
    {
        _harness.Dispose();
        return Task.CompletedTask;
    }

    private RequestWeekendLeaveCommand ValidRequest(Guid studentId) => new()
    {
        StudentId = studentId,
        DepartureDate = new DateTime(2026, 3, 7),
        ExpectedReturnDate = new DateTime(2026, 3, 8),
        Destination = "Home - Harare",
        Reason = "Family visit"
    };

    [Fact]
    public async Task A_day_scholar_cannot_request_weekend_leave()
    {
        await using var db = _harness.CreateDbContext();
        var result = await new RequestWeekendLeaveCommandHandler(db).Handle(ValidRequest(_dayScholarId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("boarding");
    }

    [Fact]
    public async Task The_full_lifecycle_moves_pending_to_returned_and_notifies_on_approval()
    {
        var channel = new FakeMessageChannel();
        Guid leaveId;

        await using (var db = _harness.CreateDbContext())
        {
            var created = await new RequestWeekendLeaveCommandHandler(db).Handle(ValidRequest(_boarderId), CancellationToken.None);
            created.IsSuccess.Should().BeTrue();
            leaveId = created.Data;
        }

        // Approve -> guardian is notified.
        await using (var db = _harness.CreateDbContext())
        {
            var review = await new ReviewWeekendLeaveCommandHandler(db, [channel], _harness.CurrentUser)
                .Handle(new ReviewWeekendLeaveCommand { LeaveId = leaveId, Approve = true }, CancellationToken.None);
            review.IsSuccess.Should().BeTrue();
        }

        channel.Sent.Should().ContainSingle();
        channel.Sent[0].Recipient.Should().Be(GuardianPhone);
        channel.Sent[0].Content.Should().Contain("Bo");

        await using (var db = _harness.CreateDbContext())
        {
            var leave = await db.WeekendLeaves.AsNoTracking().FirstAsync(l => l.Id == leaveId);
            leave.Status.Should().Be(WeekendLeaveStatuses.Approved);
            leave.GuardianNotified.Should().BeTrue();
        }

        // Sign out.
        await using (var db = _harness.CreateDbContext())
        {
            var depart = await new RecordLeaveDepartureCommandHandler(db)
                .Handle(new RecordLeaveDepartureCommand { LeaveId = leaveId, CollectedBy = "Gee Parent", DepartedAt = new DateTime(2026, 3, 7, 16, 0, 0) }, CancellationToken.None);
            depart.IsSuccess.Should().BeTrue();
        }

        await using (var db = _harness.CreateDbContext())
        {
            var leave = await db.WeekendLeaves.AsNoTracking().FirstAsync(l => l.Id == leaveId);
            leave.Status.Should().Be(WeekendLeaveStatuses.Departed);
            leave.CollectedBy.Should().Be("Gee Parent");
            leave.ActualDepartureAt.Should().NotBeNull();
        }

        // Sign back in.
        await using (var db = _harness.CreateDbContext())
        {
            var ret = await new RecordLeaveReturnCommandHandler(db)
                .Handle(new RecordLeaveReturnCommand { LeaveId = leaveId, ReturnedAt = new DateTime(2026, 3, 8, 17, 0, 0) }, CancellationToken.None);
            ret.IsSuccess.Should().BeTrue();
        }

        await using (var db = _harness.CreateDbContext())
        {
            var leave = await db.WeekendLeaves.AsNoTracking().FirstAsync(l => l.Id == leaveId);
            leave.Status.Should().Be(WeekendLeaveStatuses.Returned);
            leave.ActualReturnAt.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task Rejecting_a_request_does_not_notify_and_blocks_departure()
    {
        var channel = new FakeMessageChannel();
        Guid leaveId;

        await using (var db = _harness.CreateDbContext())
        {
            var created = await new RequestWeekendLeaveCommandHandler(db).Handle(ValidRequest(_boarderId), CancellationToken.None);
            leaveId = created.Data;
        }

        await using (var db = _harness.CreateDbContext())
        {
            await new ReviewWeekendLeaveCommandHandler(db, [channel], _harness.CurrentUser)
                .Handle(new ReviewWeekendLeaveCommand { LeaveId = leaveId, Approve = false, ReviewNote = "Fees in arrears" }, CancellationToken.None);
        }

        channel.Sent.Should().BeEmpty();

        await using (var db = _harness.CreateDbContext())
        {
            var depart = await new RecordLeaveDepartureCommandHandler(db)
                .Handle(new RecordLeaveDepartureCommand { LeaveId = leaveId, CollectedBy = "Gee Parent" }, CancellationToken.None);
            depart.IsSuccess.Should().BeFalse();
            depart.Error.Should().Contain("approved");
        }
    }

    [Fact]
    public async Task A_pending_request_cannot_be_signed_out()
    {
        Guid leaveId;
        await using (var db = _harness.CreateDbContext())
        {
            var created = await new RequestWeekendLeaveCommandHandler(db).Handle(ValidRequest(_boarderId), CancellationToken.None);
            leaveId = created.Data;
        }

        await using var db2 = _harness.CreateDbContext();
        var depart = await new RecordLeaveDepartureCommandHandler(db2)
            .Handle(new RecordLeaveDepartureCommand { LeaveId = leaveId, CollectedBy = "Someone" }, CancellationToken.None);

        depart.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Leaves_can_be_listed_and_filtered_by_status()
    {
        await using (var db = _harness.CreateDbContext())
        {
            await new RequestWeekendLeaveCommandHandler(db).Handle(ValidRequest(_boarderId), CancellationToken.None);
        }

        await using var db2 = _harness.CreateDbContext();
        var all = await new GetWeekendLeavesQueryHandler(db2).Handle(new GetWeekendLeavesQuery(), CancellationToken.None);
        all.IsSuccess.Should().BeTrue();
        all.Data!.Items.Should().ContainSingle();
        all.Data.Items[0].StudentName.Should().Contain("Bo");
        all.Data.Items[0].DormitoryName.Should().Be("East Wing");
        all.Data.Items[0].Status.Should().Be(WeekendLeaveStatuses.Pending);

        var approvedOnly = await new GetWeekendLeavesQueryHandler(db2)
            .Handle(new GetWeekendLeavesQuery { Status = WeekendLeaveStatuses.Approved }, CancellationToken.None);
        approvedOnly.Data!.Items.Should().BeEmpty();
    }
}
