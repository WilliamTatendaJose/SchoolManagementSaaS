using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Features.Hostel.Commands;
using SMS.Application.Features.Hostel.Queries;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using Xunit;

namespace SMS.Infrastructure.Tests;

public class HostelAllocationTests : IAsyncLifetime
{
    private readonly TenantTestContext _harness = new();
    private Guid _boysDormId; // capacity 1, Male
    private Guid _boyId;
    private Guid _girlId;
    private Guid _secondBoyId;

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
            var dorm = new Dormitory { Name = "Boys A", Capacity = 1, Gender = "Male", IsActive = true };
            db.Dormitories.Add(dorm);
            var boy = NewStudent("S-BOY", Gender.Male);
            var girl = NewStudent("S-GIRL", Gender.Female);
            var boy2 = NewStudent("S-BOY2", Gender.Male);
            db.Students.AddRange(boy, girl, boy2);
            await db.SaveChangesAsync();

            _boysDormId = dorm.Id;
            _boyId = boy.Id;
            _girlId = girl.Id;
            _secondBoyId = boy2.Id;
        }
    }

    public Task DisposeAsync()
    {
        _harness.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task A_matching_student_can_be_allocated()
    {
        await using var db = _harness.CreateDbContext();
        var result = await new AssignStudentToDormitoryCommandHandler(db)
            .Handle(new AssignStudentToDormitoryCommand { StudentId = _boyId, DormitoryId = _boysDormId }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await db.Students.AsNoTracking().FirstAsync(s => s.Id == _boyId)).DormitoryId.Should().Be(_boysDormId);
    }

    [Fact]
    public async Task A_gender_mismatch_is_rejected()
    {
        await using var db = _harness.CreateDbContext();
        var result = await new AssignStudentToDormitoryCommandHandler(db)
            .Handle(new AssignStudentToDormitoryCommand { StudentId = _girlId, DormitoryId = _boysDormId }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Male");
    }

    [Fact]
    public async Task A_full_dormitory_is_rejected()
    {
        // Fill the single bed with the first boy.
        await using (var db = _harness.CreateDbContext())
        {
            await new AssignStudentToDormitoryCommandHandler(db)
                .Handle(new AssignStudentToDormitoryCommand { StudentId = _boyId, DormitoryId = _boysDormId }, CancellationToken.None);
        }

        await using (var db = _harness.CreateDbContext())
        {
            var result = await new AssignStudentToDormitoryCommandHandler(db)
                .Handle(new AssignStudentToDormitoryCommand { StudentId = _secondBoyId, DormitoryId = _boysDormId }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("full");
        }
    }

    [Fact]
    public async Task Occupancy_and_available_beds_are_reported()
    {
        await using (var db = _harness.CreateDbContext())
        {
            await new AssignStudentToDormitoryCommandHandler(db)
                .Handle(new AssignStudentToDormitoryCommand { StudentId = _boyId, DormitoryId = _boysDormId }, CancellationToken.None);
        }

        await using var db2 = _harness.CreateDbContext();
        var dorms = await new GetDormitoriesQueryHandler(db2).Handle(new GetDormitoriesQuery(), CancellationToken.None);
        var dorm = dorms.Data!.Single(d => d.Id == _boysDormId);
        dorm.Occupants.Should().Be(1);
        dorm.AvailableBeds.Should().Be(0);

        var occupants = await new GetDormitoryOccupantsQueryHandler(db2).Handle(new GetDormitoryOccupantsQuery(_boysDormId), CancellationToken.None);
        occupants.Data!.Should().ContainSingle(o => o.StudentId == _boyId);
    }

    private static Student NewStudent(string number, Gender gender) => new()
    {
        StudentNumber = number,
        FirstName = number,
        LastName = "Boarder",
        DateOfBirth = new DateTime(2012, 1, 1),
        Gender = gender,
        AdmissionDate = new DateTime(2026, 1, 1),
        Status = StudentStatus.Active
    };
}
