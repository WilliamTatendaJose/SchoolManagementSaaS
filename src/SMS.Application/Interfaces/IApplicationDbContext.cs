using Microsoft.EntityFrameworkCore;
using SMS.Domain.Entities;
using DomainStream = SMS.Domain.Entities.Stream;

namespace SMS.Application.Interfaces;

/// <summary>
/// Interface for the application database context
/// </summary>
public interface IApplicationDbContext
{
    // Multi-tenancy
    DbSet<Tenant> Tenants { get; }
    
    // Identity & Access
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    
    // Student Management
    DbSet<Student> Students { get; }
    DbSet<Guardian> Guardians { get; }
    DbSet<StudentGuardian> StudentGuardians { get; }
    
    // Academic
    DbSet<AcademicYear> AcademicYears { get; }
    DbSet<AcademicTerm> AcademicTerms { get; }
    DbSet<Class> Classes { get; }
    DbSet<DomainStream> Streams { get; }
    DbSet<Subject> Subjects { get; }
    DbSet<ClassSubject> ClassSubjects { get; }
    DbSet<Enrollment> Enrollments { get; }
    
    // Staff
    DbSet<Staff> Staff { get; }
    DbSet<TeacherSubject> TeacherSubjects { get; }
    DbSet<LeaveRequest> LeaveRequests { get; }
    
    // Timetable
    DbSet<Classroom> Classrooms { get; }
    DbSet<TimetableSlot> TimetableSlots { get; }
    
    // Assessment & Results
    DbSet<Assessment> Assessments { get; }
    DbSet<Result> Results { get; }
    DbSet<ReportCardComment> ReportCardComments { get; }
    
    // Attendance
    DbSet<Attendance> Attendances { get; }
    
    // Finance
    DbSet<FeeStructure> FeeStructures { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<InvoiceItem> InvoiceItems { get; }
    DbSet<Payment> Payments { get; }
    DbSet<PaymentPlan> PaymentPlans { get; }
    DbSet<Installment> Installments { get; }
    
    // Communication
    DbSet<Message> Messages { get; }
    DbSet<MessageRecipient> MessageRecipients { get; }
    
    // Hostel
    DbSet<Dormitory> Dormitories { get; }
    DbSet<House> Houses { get; }
    DbSet<WeekendLeave> WeekendLeaves { get; }

    // Transport
    DbSet<TransportRoute> TransportRoutes { get; }
    DbSet<RouteStop> RouteStops { get; }
    
    // Discipline
    DbSet<DisciplineRecord> DisciplineRecords { get; }
    
    // Assets
    DbSet<Asset> Assets { get; }
    
    // Audit
    DbSet<AuditLog> AuditLogs { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
