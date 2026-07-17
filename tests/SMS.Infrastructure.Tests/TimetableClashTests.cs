using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Features.Timetable.Commands;
using SMS.Domain.Entities;
using Xunit;
using AcademicTermEntity = SMS.Domain.Entities.AcademicTerm;
using StaffEntity = SMS.Domain.Entities.Staff;

namespace SMS.Infrastructure.Tests;

public class TimetableClashTests : IAsyncLifetime
{
    private readonly TenantTestContext _harness = new();
    private Guid _termId, _subjectId, _classAId, _classBId, _teacher1Id, _teacher2Id, _roomId;

    public async Task InitializeAsync()
    {
        var tenant = new Tenant { Name = "Timetable School", Code = "TT" };
        await using (var db = _harness.CreateDbContext())
        {
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
        }
        _harness.UseTenant(tenant.Id);

        await using (var db = _harness.CreateDbContext())
        {
            var user1 = new User { Email = "t1@tt.zw", PasswordHash = "x", FirstName = "T", LastName = "One" };
            var user2 = new User { Email = "t2@tt.zw", PasswordHash = "x", FirstName = "T", LastName = "Two" };
            var subject = new Subject { Name = "Maths", Code = "M" };
            var classA = new Class { Name = "Form 1A", Level = 1, Capacity = 40 };
            var classB = new Class { Name = "Form 1B", Level = 1, Capacity = 40 };
            var room = new Classroom { Name = "Room 1", Capacity = 40, IsActive = true };
            var year = new AcademicYear { Name = "2026", Year = 2026, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 12, 31) };
            db.Users.AddRange(user1, user2);
            db.Subjects.Add(subject);
            db.Classes.AddRange(classA, classB);
            db.Classrooms.Add(room);
            db.AcademicYears.Add(year);
            await db.SaveChangesAsync();

            var term = new AcademicTermEntity { AcademicYearId = year.Id, Name = "Term 1", TermNumber = 1, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 4, 30) };
            db.AcademicTerms.Add(term);
            var teacher1 = new StaffEntity { UserId = user1.Id, StaffNumber = "STAFF-1", IsTeacher = true };
            var teacher2 = new StaffEntity { UserId = user2.Id, StaffNumber = "STAFF-2", IsTeacher = true };
            db.Staff.AddRange(teacher1, teacher2);
            await db.SaveChangesAsync();

            _termId = term.Id;
            _subjectId = subject.Id;
            _classAId = classA.Id;
            _classBId = classB.Id;
            _teacher1Id = teacher1.Id;
            _teacher2Id = teacher2.Id;
            _roomId = room.Id;

            // Seed the anchor lesson: Class A, Teacher 1, Room 1, Monday 08:00-09:00.
            await new CreateTimetableSlotCommandHandler(db).Handle(Slot(_classAId, _teacher1Id, _roomId, DayOfWeek.Monday, 8, 9), CancellationToken.None);
        }
    }

    public Task DisposeAsync()
    {
        _harness.Dispose();
        return Task.CompletedTask;
    }

    private CreateTimetableSlotCommand Slot(Guid classId, Guid teacherId, Guid? roomId, DayOfWeek day, int startHour, int endHour) => new()
    {
        ClassId = classId,
        SubjectId = _subjectId,
        TeacherId = teacherId,
        ClassroomId = roomId,
        AcademicTermId = _termId,
        DayOfWeek = day,
        StartTime = new TimeOnly(startHour, 0),
        EndTime = new TimeOnly(endHour, 0)
    };

    [Fact]
    public async Task Overlapping_slot_for_the_same_class_is_rejected()
    {
        await using var db = _harness.CreateDbContext();
        var result = await new CreateTimetableSlotCommandHandler(db)
            .Handle(Slot(_classAId, _teacher2Id, null, DayOfWeek.Monday, 8, 9), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("class");
    }

    [Fact]
    public async Task Overlapping_slot_for_the_same_teacher_is_rejected()
    {
        await using var db = _harness.CreateDbContext();
        var result = await new CreateTimetableSlotCommandHandler(db)
            .Handle(Slot(_classBId, _teacher1Id, null, DayOfWeek.Monday, 8, 9), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("teacher");
    }

    [Fact]
    public async Task Overlapping_slot_for_the_same_classroom_is_rejected()
    {
        await using var db = _harness.CreateDbContext();
        var result = await new CreateTimetableSlotCommandHandler(db)
            .Handle(Slot(_classBId, _teacher2Id, _roomId, DayOfWeek.Monday, 8, 9), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("classroom");
    }

    [Fact]
    public async Task Adjacent_and_different_day_slots_are_allowed()
    {
        await using var db = _harness.CreateDbContext();
        var handler = new CreateTimetableSlotCommandHandler(db);

        // Same class/teacher/room, Monday 09:00-10:00 (touches but does not overlap 08:00-09:00).
        var adjacent = await handler.Handle(Slot(_classAId, _teacher1Id, _roomId, DayOfWeek.Monday, 9, 10), CancellationToken.None);
        adjacent.IsSuccess.Should().BeTrue();

        // Same everything but Tuesday 08:00-09:00.
        var otherDay = await handler.Handle(Slot(_classAId, _teacher1Id, _roomId, DayOfWeek.Tuesday, 8, 9), CancellationToken.None);
        otherDay.IsSuccess.Should().BeTrue();
    }
}
