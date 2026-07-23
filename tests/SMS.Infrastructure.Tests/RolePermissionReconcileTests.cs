using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Security;
using SMS.Domain.Entities;
using SMS.Infrastructure.Persistence;
using Xunit;

namespace SMS.Infrastructure.Tests;

/// <summary>
/// Locks in the fix for the "seed only runs on an empty roles table" bug: a change to the
/// Parent/Student roles' permission set (now empty) must reach a database that was seeded
/// before the change, so a parent can never keep a stale staff permission like
/// <c>finance.view</c> that would let them read every family's data via the API.
/// </summary>
public class RolePermissionReconcileTests : IDisposable
{
    private readonly TenantTestContext _harness = new();

    public void Dispose() => _harness.Dispose();

    private async Task<Guid> SeedPermissionAsync(ApplicationDbContext db, string code)
    {
        var permission = new Permission { Name = code, Code = code, Module = "Test" };
        db.Permissions.Add(permission);
        await db.SaveChangesAsync();
        return permission.Id;
    }

    [Fact]
    public async Task Reconcile_strips_stale_staff_permissions_from_the_parent_role()
    {
        await using (var db = _harness.CreateDbContext())
        {
            var financeView = await SeedPermissionAsync(db, Permissions.FinanceView);
            var studentsView = await SeedPermissionAsync(db, Permissions.StudentsView);

            var parent = new Role { Name = DefaultRoles.Parent, IsSystemRole = true };
            db.Roles.Add(parent);
            await db.SaveChangesAsync();

            // Simulate the pre-existing database: the Parent role was seeded long ago with
            // staff view permissions it should never have had.
            db.RolePermissions.Add(new RolePermission { RoleId = parent.Id, PermissionId = financeView });
            db.RolePermissions.Add(new RolePermission { RoleId = parent.Id, PermissionId = studentsView });
            await db.SaveChangesAsync();
        }

        await using (var db = _harness.CreateDbContext())
        {
            await DatabaseSeeder.ReconcileLockedRolePermissionsAsync(db);
        }

        await using var verify = _harness.CreateDbContext();
        var parentRole = await verify.Roles.FirstAsync(r => r.Name == DefaultRoles.Parent);
        (await verify.RolePermissions.CountAsync(rp => rp.RoleId == parentRole.Id))
            .Should().Be(0, "the Parent role must carry no permissions - the portal is ownership-scoped");
    }

    [Fact]
    public async Task Reconcile_leaves_staff_role_permissions_untouched()
    {
        Guid teacherRoleId;
        await using (var db = _harness.CreateDbContext())
        {
            var studentsView = await SeedPermissionAsync(db, Permissions.StudentsView);

            // A staff role an admin may have customised - reconcile must not touch it.
            var teacher = new Role { Name = DefaultRoles.Teacher, IsSystemRole = true };
            db.Roles.Add(teacher);
            await db.SaveChangesAsync();
            teacherRoleId = teacher.Id;

            db.RolePermissions.Add(new RolePermission { RoleId = teacher.Id, PermissionId = studentsView });
            await db.SaveChangesAsync();
        }

        await using (var db = _harness.CreateDbContext())
        {
            await DatabaseSeeder.ReconcileLockedRolePermissionsAsync(db);
        }

        await using var verify = _harness.CreateDbContext();
        (await verify.RolePermissions.CountAsync(rp => rp.RoleId == teacherRoleId))
            .Should().Be(1, "staff roles are left to admin customisation, not force-reconciled");
    }

    [Fact]
    public async Task Reconcile_is_idempotent_when_the_parent_role_is_already_clean()
    {
        await using (var db = _harness.CreateDbContext())
        {
            db.Roles.Add(new Role { Name = DefaultRoles.Parent, IsSystemRole = true });
            await db.SaveChangesAsync();
        }

        await using (var db = _harness.CreateDbContext())
        {
            await DatabaseSeeder.ReconcileLockedRolePermissionsAsync(db);
            await DatabaseSeeder.ReconcileLockedRolePermissionsAsync(db);
        }

        await using var verify = _harness.CreateDbContext();
        var parentRole = await verify.Roles.FirstAsync(r => r.Name == DefaultRoles.Parent);
        (await verify.RolePermissions.CountAsync(rp => rp.RoleId == parentRole.Id)).Should().Be(0);
    }
}
