using SMS.Domain.Entities;
using SMS.Domain.Enums;
using Xunit;

namespace SMS.Infrastructure.Tests;

/// <summary>
/// Shared fixture seeding two tenants, each with one student and one guardian.
/// A single fixture instance is reused across the test class so that every context
/// shares the one <see cref="Services.TenantService"/> instance baked into EF's cached
/// model (otherwise the tenant filter would reference a stale service).
///
/// Seeding runs through <see cref="ApplicationDbContext.SaveChangesAsync"/> - the same
/// path the request pipeline uses - so TenantId stamping is exercised, not bypassed.
/// </summary>
public sealed class TenantIsolationFixture : IAsyncLifetime
{
    private readonly TenantTestContext _harness = new();

    public Guid TenantAId { get; private set; }
    public Guid TenantBId { get; private set; }
    public Guid StudentAId { get; private set; }
    public Guid StudentBId { get; private set; }

    public const string StudentANumber = "STU-A-0001";
    public const string StudentBNumber = "STU-B-0001";

    internal TenantTestContext Harness => _harness;

    public async Task InitializeAsync()
    {
        // Tenants are not tenant-scoped, so create them with no active tenant.
        var tenantA = new Tenant { Name = "Alpha Academy", Code = "ALPHA" };
        var tenantB = new Tenant { Name = "Beta School", Code = "BETA" };
        await using (var db = _harness.CreateDbContext())
        {
            db.Tenants.AddRange(tenantA, tenantB);
            await db.SaveChangesAsync();
        }

        TenantAId = tenantA.Id;
        TenantBId = tenantB.Id;

        _harness.UseTenant(TenantAId);
        var studentA = NewStudent(StudentANumber, "Alice", "Alpha");
        await using (var db = _harness.CreateDbContext())
        {
            db.Students.Add(studentA);
            db.Guardians.Add(NewGuardian("Anna", "Alpha"));
            await db.SaveChangesAsync();
        }
        StudentAId = studentA.Id;

        _harness.UseTenant(TenantBId);
        var studentB = NewStudent(StudentBNumber, "Bob", "Beta");
        await using (var db = _harness.CreateDbContext())
        {
            db.Students.Add(studentB);
            db.Guardians.Add(NewGuardian("Ben", "Beta"));
            await db.SaveChangesAsync();
        }
        StudentBId = studentB.Id;
    }

    public Task DisposeAsync()
    {
        _harness.Dispose();
        return Task.CompletedTask;
    }

    public static Student NewStudent(string number, string first, string last) => new()
    {
        StudentNumber = number,
        FirstName = first,
        LastName = last,
        DateOfBirth = new DateTime(2012, 1, 1),
        Gender = Gender.Other,
        AdmissionDate = new DateTime(2024, 1, 1),
        Status = StudentStatus.Active
    };

    public static Guardian NewGuardian(string first, string last) => new()
    {
        FirstName = first,
        LastName = last,
        Gender = Gender.Other
    };
}
