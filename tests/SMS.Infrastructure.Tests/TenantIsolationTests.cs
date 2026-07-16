using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace SMS.Infrastructure.Tests;

/// <summary>
/// Verifies the global tenant query filter and TenantId stamping actually isolate
/// data between tenants - the highest-risk failure mode of the SaaS. Each test creates
/// a fresh context after selecting the tenant, mirroring the per-request scoped context.
/// </summary>
public class TenantIsolationTests : IClassFixture<TenantIsolationFixture>
{
    private readonly TenantIsolationFixture _fixture;

    public TenantIsolationTests(TenantIsolationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Students_query_returns_only_the_current_tenants_rows()
    {
        _fixture.Harness.UseTenant(_fixture.TenantAId);
        await using var db = _fixture.Harness.CreateDbContext();

        var students = await db.Students.AsNoTracking().ToListAsync();

        students.Should().OnlyContain(s => s.TenantId == _fixture.TenantAId);
        students.Should().Contain(s => s.StudentNumber == TenantIsolationFixture.StudentANumber);
        students.Should().NotContain(s => s.StudentNumber == TenantIsolationFixture.StudentBNumber);
    }

    [Fact]
    public async Task Switching_tenant_switches_the_visible_rows()
    {
        _fixture.Harness.UseTenant(_fixture.TenantBId);
        await using var db = _fixture.Harness.CreateDbContext();

        var students = await db.Students.AsNoTracking().ToListAsync();

        students.Should().OnlyContain(s => s.TenantId == _fixture.TenantBId);
        students.Should().Contain(s => s.StudentNumber == TenantIsolationFixture.StudentBNumber);
        students.Should().NotContain(s => s.StudentNumber == TenantIsolationFixture.StudentANumber);
    }

    [Fact]
    public async Task Another_tenants_row_cannot_be_fetched_by_id()
    {
        // Active tenant B trying to reach tenant A's student - the basis of any
        // cross-tenant update or delete attempt.
        _fixture.Harness.UseTenant(_fixture.TenantBId);
        await using var db = _fixture.Harness.CreateDbContext();

        var foreignStudent = await db.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == _fixture.StudentAId);

        foreignStudent.Should().BeNull();
    }

    [Fact]
    public async Task SaveChanges_stamps_the_current_tenant_id_on_new_rows()
    {
        _fixture.Harness.UseTenant(_fixture.TenantAId);
        await using var db = _fixture.Harness.CreateDbContext();

        var student = TenantIsolationFixture.NewStudent("STU-A-STAMP", "Stamp", "Test");
        db.Students.Add(student);
        await db.SaveChangesAsync();

        student.TenantId.Should().Be(_fixture.TenantAId);
    }

    [Fact]
    public async Task Guardians_are_tenant_scoped_even_without_a_dedicated_config_filter()
    {
        // Guardian has no per-entity query filter in its configuration, so this proves
        // the tenant filter is applied uniformly to every ITenantEntity.
        _fixture.Harness.UseTenant(_fixture.TenantAId);
        await using (var dbA = _fixture.Harness.CreateDbContext())
        {
            var tenantAGuardians = await dbA.Guardians.AsNoTracking().ToListAsync();
            tenantAGuardians.Should().OnlyContain(g => g.TenantId == _fixture.TenantAId);
            tenantAGuardians.Should().Contain(g => g.FirstName == "Anna");
        }

        _fixture.Harness.UseTenant(_fixture.TenantBId);
        await using (var dbB = _fixture.Harness.CreateDbContext())
        {
            var tenantBGuardians = await dbB.Guardians.AsNoTracking().ToListAsync();
            tenantBGuardians.Should().OnlyContain(g => g.TenantId == _fixture.TenantBId);
            tenantBGuardians.Should().NotContain(g => g.FirstName == "Anna");
        }
    }

    [Fact]
    public async Task IgnoreQueryFilters_can_still_see_across_tenants_for_admin_scenarios()
    {
        // Sanity check that isolation is enforced by the filter (not by missing data):
        // bypassing the filter surfaces both tenants' rows.
        _fixture.Harness.UseTenant(_fixture.TenantAId);
        await using var db = _fixture.Harness.CreateDbContext();

        var allStudents = await db.Students
            .IgnoreQueryFilters()
            .AsNoTracking()
            .ToListAsync();

        allStudents.Should().Contain(s => s.TenantId == _fixture.TenantAId);
        allStudents.Should().Contain(s => s.TenantId == _fixture.TenantBId);
    }
}
