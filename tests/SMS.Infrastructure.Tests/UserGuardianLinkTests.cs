using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Features.ParentPortal.Queries;
using SMS.Application.Features.Users.Commands;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using Xunit;

namespace SMS.Infrastructure.Tests;

/// <summary>
/// Covers linking an existing login to a guardian via UpdateUser (the gap that made the
/// parent portal show no children even when a guardian was linked to a student) - the
/// link is the Guardian.UserId FK that ParentChildAccess resolves through.
/// </summary>
public class UserGuardianLinkTests : IAsyncLifetime
{
    private readonly TenantTestContext _harness = new();

    private Guid _userId;
    private Guid _guardianId;
    private Guid _studentId;

    public async Task InitializeAsync()
    {
        var tenant = new Tenant { Name = "Link School", Code = "LNK" };
        await using (var db = _harness.CreateDbContext())
        {
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
        }
        _harness.UseTenant(tenant.Id);

        await using (var db = _harness.CreateDbContext())
        {
            var user = new User { Email = "parent@lnk.zw", PasswordHash = "x", FirstName = "Pat", LastName = "Parent" };
            var guardian = new Guardian { FirstName = "Pat", LastName = "Parent", Gender = Gender.Female, Phone = "+263771111111" };
            var student = new Student
            {
                StudentNumber = "S-LNK-1", FirstName = "Rufaro", LastName = "Nyathi",
                DateOfBirth = new DateTime(2013, 1, 1), Gender = Gender.Male,
                AdmissionDate = new DateTime(2026, 1, 1), Status = StudentStatus.Active
            };
            db.Users.Add(user);
            db.Guardians.Add(guardian);
            db.Students.Add(student);
            await db.SaveChangesAsync();

            // Guardian is linked to the student, but NOT to the login (Guardian.UserId is null) -
            // exactly the reported state.
            db.StudentGuardians.Add(new StudentGuardian { StudentId = student.Id, GuardianId = guardian.Id, Relationship = "Mother", IsPrimaryContact = true });
            await db.SaveChangesAsync();

            _userId = user.Id;
            _guardianId = guardian.Id;
            _studentId = student.Id;
            _harness.CurrentUser.UserId = user.Id;
        }
    }

    private async Task<List<Guid>> PortalChildIdsAsync()
    {
        await using var db = _harness.CreateDbContext();
        var result = await new GetMyChildrenQueryHandler(db, _harness.CurrentUser)
            .Handle(new GetMyChildrenQuery(), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        return result.Data!.Select(c => c.StudentId).ToList();
    }

    public Task DisposeAsync()
    {
        _harness.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Before_linking_the_portal_finds_no_children()
    {
        (await PortalChildIdsAsync()).Should().BeEmpty("the guardian isn't linked to the login yet");
    }

    [Fact]
    public async Task Linking_the_guardian_to_the_login_makes_the_child_visible()
    {
        await using (var db = _harness.CreateDbContext())
        {
            var result = await new UpdateUserCommandHandler(db).Handle(new UpdateUserCommand
            {
                Id = _userId,
                FirstName = "Pat",
                LastName = "Parent",
                IsActive = true,
                GuardianId = _guardianId
            }, CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
        }

        await using (var verify = _harness.CreateDbContext())
        {
            (await verify.Guardians.AsNoTracking().FirstAsync(g => g.Id == _guardianId)).UserId.Should().Be(_userId);
        }

        (await PortalChildIdsAsync()).Should().ContainSingle().Which.Should().Be(_studentId);
    }

    [Fact]
    public async Task Clearing_the_guardian_id_unlinks_the_login()
    {
        // First link it.
        await using (var db = _harness.CreateDbContext())
        {
            await new UpdateUserCommandHandler(db).Handle(new UpdateUserCommand
            { Id = _userId, FirstName = "Pat", LastName = "Parent", IsActive = true, GuardianId = _guardianId }, CancellationToken.None);
        }

        // Then clear it.
        await using (var db = _harness.CreateDbContext())
        {
            await new UpdateUserCommandHandler(db).Handle(new UpdateUserCommand
            { Id = _userId, FirstName = "Pat", LastName = "Parent", IsActive = true, GuardianId = null }, CancellationToken.None);
        }

        await using (var verify = _harness.CreateDbContext())
        {
            (await verify.Guardians.AsNoTracking().FirstAsync(g => g.Id == _guardianId)).UserId.Should().BeNull();
        }
        (await PortalChildIdsAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Relinking_to_a_different_guardian_moves_the_link()
    {
        Guid otherGuardianId;
        await using (var db = _harness.CreateDbContext())
        {
            var other = new Guardian { FirstName = "Other", LastName = "Guardian", Gender = Gender.Male };
            db.Guardians.Add(other);
            await db.SaveChangesAsync();
            otherGuardianId = other.Id;

            await new UpdateUserCommandHandler(db).Handle(new UpdateUserCommand
            { Id = _userId, FirstName = "Pat", LastName = "Parent", IsActive = true, GuardianId = _guardianId }, CancellationToken.None);
        }

        await using (var db = _harness.CreateDbContext())
        {
            await new UpdateUserCommandHandler(db).Handle(new UpdateUserCommand
            { Id = _userId, FirstName = "Pat", LastName = "Parent", IsActive = true, GuardianId = otherGuardianId }, CancellationToken.None);
        }

        await using var verify = _harness.CreateDbContext();
        (await verify.Guardians.AsNoTracking().FirstAsync(g => g.Id == _guardianId)).UserId.Should().BeNull("the link moved off the first guardian");
        (await verify.Guardians.AsNoTracking().FirstAsync(g => g.Id == otherGuardianId)).UserId.Should().Be(_userId);
    }
}
