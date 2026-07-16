using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SMS.Domain.Entities;
using Xunit;

namespace SMS.Infrastructure.Tests;

/// <summary>
/// Guards two DbContext behaviours: synchronous SaveChanges must apply the same
/// tenant/audit/soft-delete rules as the async path, and soft-deleted rows must be hidden
/// by the global filter (while still being retained in the database).
/// </summary>
public class DbContextBehaviorTests : IAsyncLifetime
{
    private readonly TenantTestContext _harness = new();
    private Guid _tenantId;

    public async Task InitializeAsync()
    {
        var tenant = new Tenant { Name = "Behavior School", Code = "BEH" };
        await using var db = _harness.CreateDbContext();
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        _tenantId = tenant.Id;
        _harness.UseTenant(_tenantId);
    }

    public Task DisposeAsync()
    {
        _harness.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public void Synchronous_SaveChanges_stamps_the_current_tenant()
    {
        // Before the fix, only SaveChangesAsync stamped TenantId, so this synchronous save
        // left TenantId empty and tripped the FK to Tenants.
        using var db = _harness.CreateDbContext();
        var student = TenantIsolationFixture.NewStudent("STU-SYNC-1", "Sync", "Saver");
        db.Students.Add(student);

        var act = () => db.SaveChanges();

        act.Should().NotThrow();
        student.TenantId.Should().Be(_tenantId);
    }

    [Fact]
    public async Task Synchronous_delete_is_converted_to_a_soft_delete()
    {
        Guid id;
        await using (var db = _harness.CreateDbContext())
        {
            var student = TenantIsolationFixture.NewStudent("STU-SYNC-DEL", "Delete", "Me");
            db.Students.Add(student);
            await db.SaveChangesAsync();
            id = student.Id;

            db.Students.Remove(student);
            db.SaveChanges(); // synchronous
        }

        await using var verify = _harness.CreateDbContext();
        (await verify.Students.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id))
            .Should().BeNull("a soft-deleted row is hidden by the filter");

        var raw = await verify.Students.IgnoreQueryFilters().AsNoTracking().FirstAsync(s => s.Id == id);
        raw.IsDeleted.Should().BeTrue("the row is retained, not physically removed");
    }

    [Fact]
    public async Task Soft_deleted_row_is_hidden_even_without_a_per_config_filter()
    {
        // Guardian has no per-config query filter, so this proves soft-delete is applied
        // centrally alongside tenant isolation for every tenant entity.
        Guid id;
        await using (var db = _harness.CreateDbContext())
        {
            var guardian = TenantIsolationFixture.NewGuardian("Soft", "Deleted");
            db.Guardians.Add(guardian);
            await db.SaveChangesAsync();
            id = guardian.Id;

            db.Guardians.Remove(guardian);
            await db.SaveChangesAsync();
        }

        await using var verify = _harness.CreateDbContext();
        (await verify.Guardians.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id))
            .Should().BeNull();

        var raw = await verify.Guardians.IgnoreQueryFilters().AsNoTracking().FirstAsync(g => g.Id == id);
        raw.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Active_rows_remain_visible_after_a_sibling_is_soft_deleted()
    {
        await using (var db = _harness.CreateDbContext())
        {
            db.Students.Add(TenantIsolationFixture.NewStudent("STU-KEEP", "Keep", "Visible"));
            db.Students.Add(TenantIsolationFixture.NewStudent("STU-GONE", "Gone", "Soon"));
            await db.SaveChangesAsync();

            var gone = await db.Students.FirstAsync(s => s.StudentNumber == "STU-GONE");
            db.Students.Remove(gone);
            await db.SaveChangesAsync();
        }

        await using var verify = _harness.CreateDbContext();
        var visible = await verify.Students.AsNoTracking().ToListAsync();

        visible.Should().Contain(s => s.StudentNumber == "STU-KEEP");
        visible.Should().NotContain(s => s.StudentNumber == "STU-GONE");
    }
}
