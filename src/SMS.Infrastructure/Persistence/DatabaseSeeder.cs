using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SMS.Application.Common.Security;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using UserRoleEntity = SMS.Domain.Entities.UserRole;

namespace SMS.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await context.Database.MigrateAsync();

        await SeedPermissionsAsync(context);
        await SeedRolesAsync(context);
        await SeedDefaultTenantAsync(context);
    }

    private static async Task SeedPermissionsAsync(ApplicationDbContext context)
    {
        if (await context.Permissions.AnyAsync())
            return;

        var permissions = new List<Permission>
        {
            // Students
            new() { Name = "View Students", Code = Permissions.StudentsView, Module = "Students", Description = "View student records" },
            new() { Name = "Create Students", Code = Permissions.StudentsCreate, Module = "Students", Description = "Create new students" },
            new() { Name = "Edit Students", Code = Permissions.StudentsEdit, Module = "Students", Description = "Edit student records" },
            new() { Name = "Delete Students", Code = Permissions.StudentsDelete, Module = "Students", Description = "Delete students" },
            new() { Name = "Export Students", Code = Permissions.StudentsExport, Module = "Students", Description = "Export student data" },

            // Guardians
            new() { Name = "View Guardians", Code = Permissions.GuardiansView, Module = "Guardians", Description = "View guardian records" },
            new() { Name = "Create Guardians", Code = Permissions.GuardiansCreate, Module = "Guardians", Description = "Create new guardians" },
            new() { Name = "Edit Guardians", Code = Permissions.GuardiansEdit, Module = "Guardians", Description = "Edit guardian records" },
            new() { Name = "Delete Guardians", Code = Permissions.GuardiansDelete, Module = "Guardians", Description = "Delete guardians" },

            // Enrollment
            new() { Name = "View Enrollments", Code = Permissions.EnrollmentsView, Module = "Enrollment", Description = "View student enrollments" },
            new() { Name = "Manage Enrollments", Code = Permissions.EnrollmentsManage, Module = "Enrollment", Description = "Enroll, transfer and withdraw students" },
            new() { Name = "Promote Students", Code = Permissions.EnrollmentsPromote, Module = "Enrollment", Description = "Bulk promote students to a new class/year" },

            // Academic
            new() { Name = "View Classes", Code = Permissions.ClassesView, Module = "Academic", Description = "View class information" },
            new() { Name = "Manage Classes", Code = Permissions.ClassesManage, Module = "Academic", Description = "Create/edit classes" },
            new() { Name = "View Subjects", Code = Permissions.SubjectsView, Module = "Academic", Description = "View subjects" },
            new() { Name = "Manage Subjects", Code = Permissions.SubjectsManage, Module = "Academic", Description = "Create/edit subjects" },
            new() { Name = "View Timetable", Code = Permissions.TimetableView, Module = "Academic", Description = "View timetable" },
            new() { Name = "Manage Timetable", Code = Permissions.TimetableManage, Module = "Academic", Description = "Create/edit timetable" },

            // Assessment
            new() { Name = "View Assessments", Code = Permissions.AssessmentsView, Module = "Assessment", Description = "View assessments" },
            new() { Name = "Create Assessments", Code = Permissions.AssessmentsCreate, Module = "Assessment", Description = "Create assessments" },
            new() { Name = "Edit Assessments", Code = Permissions.AssessmentsEdit, Module = "Assessment", Description = "Edit assessments" },
            new() { Name = "View Results", Code = Permissions.ResultsView, Module = "Assessment", Description = "View student results" },
            new() { Name = "Record Results", Code = Permissions.ResultsRecord, Module = "Assessment", Description = "Record student results" },
            new() { Name = "Publish Results", Code = Permissions.ResultsPublish, Module = "Assessment", Description = "Publish results" },
            new() { Name = "Moderate Results", Code = Permissions.ResultsModerate, Module = "Assessment", Description = "Moderate/adjust results" },

            // Attendance
            new() { Name = "View Attendance", Code = Permissions.AttendanceView, Module = "Attendance", Description = "View attendance records" },
            new() { Name = "Mark Attendance", Code = Permissions.AttendanceMark, Module = "Attendance", Description = "Mark attendance" },
            new() { Name = "Attendance Reports", Code = Permissions.AttendanceReport, Module = "Attendance", Description = "Generate attendance reports" },

            // Discipline
            new() { Name = "View Discipline", Code = Permissions.DisciplineView, Module = "Discipline", Description = "View discipline records" },
            new() { Name = "Manage Discipline", Code = Permissions.DisciplineManage, Module = "Discipline", Description = "Record and manage discipline incidents" },

            // Finance
            new() { Name = "View Finance", Code = Permissions.FinanceView, Module = "Finance", Description = "View financial data" },
            new() { Name = "View Fee Structures", Code = Permissions.FeeStructuresView, Module = "Finance", Description = "View fee structures" },
            new() { Name = "Manage Fee Structures", Code = Permissions.FeeStructuresManage, Module = "Finance", Description = "Create/edit fee structures" },
            new() { Name = "Create Invoices", Code = Permissions.InvoicesCreate, Module = "Finance", Description = "Create invoices" },
            new() { Name = "Edit Invoices", Code = Permissions.InvoicesEdit, Module = "Finance", Description = "Edit invoices" },
            new() { Name = "Record Payments", Code = Permissions.PaymentsRecord, Module = "Finance", Description = "Record payments" },
            new() { Name = "Refund Payments", Code = Permissions.PaymentsRefund, Module = "Finance", Description = "Process refunds" },
            new() { Name = "Finance Reports", Code = Permissions.FinanceReport, Module = "Finance", Description = "Generate finance reports" },

            // Communication
            new() { Name = "View Messages", Code = Permissions.MessagesView, Module = "Communication", Description = "View messages" },
            new() { Name = "Send Messages", Code = Permissions.MessagesSend, Module = "Communication", Description = "Send messages" },
            new() { Name = "Bulk Messages", Code = Permissions.MessagesBulk, Module = "Communication", Description = "Send bulk messages" },

            // Staff
            new() { Name = "View Staff", Code = Permissions.StaffView, Module = "Staff", Description = "View staff records" },
            new() { Name = "Create Staff", Code = Permissions.StaffCreate, Module = "Staff", Description = "Create staff members" },
            new() { Name = "Edit Staff", Code = Permissions.StaffEdit, Module = "Staff", Description = "Edit staff records" },
            new() { Name = "Delete Staff", Code = Permissions.StaffDelete, Module = "Staff", Description = "Delete staff members" },
            new() { Name = "Approve Leave", Code = Permissions.LeaveApprove, Module = "Staff", Description = "Approve leave requests" },

            // Users
            new() { Name = "View Users", Code = Permissions.UsersView, Module = "Users", Description = "View user accounts" },
            new() { Name = "Create Users", Code = Permissions.UsersCreate, Module = "Users", Description = "Create user accounts" },
            new() { Name = "Edit Users", Code = Permissions.UsersEdit, Module = "Users", Description = "Edit user accounts" },
            new() { Name = "Delete Users", Code = Permissions.UsersDelete, Module = "Users", Description = "Delete user accounts" },
            new() { Name = "Manage Roles", Code = Permissions.RolesManage, Module = "Users", Description = "Manage roles and permissions" },

            // System
            new() { Name = "View Settings", Code = Permissions.SettingsView, Module = "System", Description = "View system settings" },
            new() { Name = "Manage Settings", Code = Permissions.SettingsManage, Module = "System", Description = "Modify system settings" },
            new() { Name = "View Audit Logs", Code = Permissions.AuditLogsView, Module = "System", Description = "View audit logs" },
            new() { Name = "Manage Tenant", Code = Permissions.TenantManage, Module = "System", Description = "Manage tenant settings" },

            // Reports
            new() { Name = "Academic Reports", Code = Permissions.ReportsAcademic, Module = "Reports", Description = "Generate academic reports" },
            new() { Name = "Finance Reports", Code = Permissions.ReportsFinance, Module = "Reports", Description = "Generate finance reports" },
            new() { Name = "Attendance Reports", Code = Permissions.ReportsAttendance, Module = "Reports", Description = "Generate attendance reports" },
            new() { Name = "Dashboard", Code = Permissions.ReportsDashboard, Module = "Reports", Description = "View dashboard" }
        };

        context.Permissions.AddRange(permissions);
        await context.SaveChangesAsync();
    }

    private static async Task SeedRolesAsync(ApplicationDbContext context)
    {
        if (await context.Roles.AnyAsync())
            return;

        var permissions = await context.Permissions.ToListAsync();
        var rolePermissions = DefaultRoles.GetRolePermissions();

        var roles = new List<Role>
        {
            new() { Name = DefaultRoles.SuperAdmin, Description = "System administrator with full access", IsSystemRole = true },
            new() { Name = DefaultRoles.SchoolAdmin, Description = "School administrator", IsSystemRole = true },
            new() { Name = DefaultRoles.HeadTeacher, Description = "Head teacher/Principal", IsSystemRole = true },
            new() { Name = DefaultRoles.DeputyHead, Description = "Deputy head teacher", IsSystemRole = true },
            new() { Name = DefaultRoles.Teacher, Description = "Teaching staff", IsSystemRole = true },
            new() { Name = DefaultRoles.Bursar, Description = "Finance officer", IsSystemRole = true },
            new() { Name = DefaultRoles.Librarian, Description = "Library staff", IsSystemRole = false },
            new() { Name = DefaultRoles.HostelWarden, Description = "Hostel management", IsSystemRole = false },
            new() { Name = DefaultRoles.Parent, Description = "Parent/Guardian", IsSystemRole = true },
            new() { Name = DefaultRoles.Student, Description = "Student", IsSystemRole = true }
        };

        // First, add all roles
        foreach (var role in roles)
        {
            context.Roles.Add(role);
        }
        
        await context.SaveChangesAsync();

        // Then, add role permissions after roles are saved
        foreach (var role in roles)
        {
            if (rolePermissions.TryGetValue(role.Name, out var permCodes))
            {
                foreach (var permCode in permCodes)
                {
                    var permission = permissions.FirstOrDefault(p => p.Code == permCode);
                    if (permission != null)
                    {
                        // Check if this role-permission mapping doesn't already exist
                        var existingMapping = await context.RolePermissions
                            .AnyAsync(rp => rp.RoleId == role.Id && rp.PermissionId == permission.Id);
                        
                        if (!existingMapping)
                        {
                            context.RolePermissions.Add(new RolePermission
                            {
                                RoleId = role.Id,
                                PermissionId = permission.Id
                            });
                        }
                    }
                }
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedDefaultTenantAsync(ApplicationDbContext context)
    {
        if (await context.Tenants.AnyAsync())
            return;

        // Create a default demo tenant
        var tenant = new Tenant
        {
            Name = "Demo School",
            Code = "DEMO001",
            Email = "admin@demoschool.com",
            Phone = "+263 77 123 4567",
            Address = "123 School Road",
            City = "Harare",
            Country = "Zimbabwe",
            Status = TenantStatus.Active,
            SubscriptionPlan = SubscriptionPlan.Premium,
            MaxStudents = 500,
            SubscriptionStartDate = DateTime.UtcNow,
            SubscriptionEndDate = DateTime.UtcNow.AddYears(1)
        };

        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        // Create a default admin user for the tenant
        var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == DefaultRoles.SchoolAdmin);

        var adminUser = new User
        {
            TenantId = tenant.Id,
            Email = "admin@demoschool.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            FirstName = "System",
            LastName = "Administrator",
            IsActive = true,
            EmailConfirmed = true
        };

        context.Users.Add(adminUser);
        await context.SaveChangesAsync();

        if (adminRole != null)
        {
            context.UserRoles.Add(new UserRoleEntity
            {
                TenantId = tenant.Id,  // Set the TenantId!
                UserId = adminUser.Id,
                RoleId = adminRole.Id
            });
            await context.SaveChangesAsync();
        }
    }
}
