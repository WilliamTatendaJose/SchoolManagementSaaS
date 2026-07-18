using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SMS.Application.Interfaces;
using SMS.Domain.Common;
using SMS.Domain.Entities;
using DomainStream = SMS.Domain.Entities.Stream;

namespace SMS.Infrastructure.Persistence;

/// <summary>
/// Main application database context with multi-tenancy support
/// </summary>
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ITenantService tenantService,
        ICurrentUserService currentUserService) : base(options)
    {
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    // Multi-tenancy
    public DbSet<Tenant> Tenants => Set<Tenant>();

    // Identity & Access
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    // Student Management
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Guardian> Guardians => Set<Guardian>();
    public DbSet<StudentGuardian> StudentGuardians => Set<StudentGuardian>();

    // Academic
    public DbSet<AcademicYear> AcademicYears => Set<AcademicYear>();
    public DbSet<AcademicTerm> AcademicTerms => Set<AcademicTerm>();
    public DbSet<Class> Classes => Set<Class>();
    public DbSet<DomainStream> Streams => Set<DomainStream>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<ClassSubject> ClassSubjects => Set<ClassSubject>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();

    // Staff
    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<TeacherSubject> TeacherSubjects => Set<TeacherSubject>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();

    // Timetable
    public DbSet<Classroom> Classrooms => Set<Classroom>();
    public DbSet<TimetableSlot> TimetableSlots => Set<TimetableSlot>();

    // Assessment & Results
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<Result> Results => Set<Result>();
    public DbSet<ReportCardComment> ReportCardComments => Set<ReportCardComment>();

    // Attendance
    public DbSet<Attendance> Attendances => Set<Attendance>();

    // Finance
    public DbSet<FeeStructure> FeeStructures => Set<FeeStructure>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentPlan> PaymentPlans => Set<PaymentPlan>();
    public DbSet<Installment> Installments => Set<Installment>();

    // Communication
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<MessageRecipient> MessageRecipients => Set<MessageRecipient>();

    // Hostel
    public DbSet<Dormitory> Dormitories => Set<Dormitory>();
    public DbSet<House> Houses => Set<House>();
    public DbSet<WeekendLeave> WeekendLeaves => Set<WeekendLeave>();

    // Transport
    public DbSet<TransportRoute> TransportRoutes => Set<TransportRoute>();
    public DbSet<RouteStop> RouteStops => Set<RouteStop>();

    // Library
    public DbSet<Book> Books => Set<Book>();
    public DbSet<BookLoan> BookLoans => Set<BookLoan>();

    // LMS-lite
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<AssignmentSubmission> AssignmentSubmissions => Set<AssignmentSubmission>();

    // Discipline
    public DbSet<DisciplineRecord> DisciplineRecords => Set<DisciplineRecord>();

    // Assets
    public DbSet<Asset> Assets => Set<Asset>();

    // Audit
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    /// <summary>
    /// The tenant scoping the current context. Referenced by the global query filter so
    /// that EF Core re-evaluates it against the executing context on every query, rather
    /// than baking a single tenant into the cached model (which would leak data across
    /// tenants once the model is first built).
    /// </summary>
    public Guid? CurrentTenantId => _tenantService.GetCurrentTenantId();

    // Npgsql requires DateTimeKind.Utc for "timestamp with time zone" columns (every DateTime
    // column here, via the Postgres provider's default mapping). Dates coming in over JSON -
    // e.g. a plain "2014-05-12" date-of-birth from the frontend - deserialize with
    // Kind=Unspecified, which Npgsql now rejects outright instead of assuming UTC. These
    // fields are calendar dates/timestamps with no real timezone semantics of their own, so
    // relabeling (not shifting) to UTC on write is correct; reads from timestamptz already
    // come back UTC-kind, but SpecifyKind again defensively in case of e.g. DateTime.MinValue.
    private static readonly ValueConverter<DateTime, DateTime> UtcDateTimeConverter = new(
        v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    private static readonly ValueConverter<DateTime?, DateTime?> UtcNullableDateTimeConverter = new(
        v => v.HasValue && v.Value.Kind != DateTimeKind.Utc ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v,
        v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Apply global tenant filter for all tenant entities
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(ApplicationDbContext)
                    .GetMethod(nameof(ApplyTenantFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .MakeGenericMethod(entityType.ClrType);

                method.Invoke(this, [modelBuilder]);
            }

            // Force every DateTime/DateTime? column to be treated as UTC.
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(UtcDateTimeConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(UtcNullableDateTimeConverter);
                }
            }
        }
    }

    private void ApplyTenantFilter<T>(ModelBuilder modelBuilder) where T : BaseEntity, ITenantEntity
    {
        // Combine tenant isolation and soft-delete into a single filter. EF Core replaces
        // an entity's unnamed query filter on each HasQueryFilter call, so a per-config
        // "!IsDeleted" filter would otherwise be dropped when this tenant filter is applied.
        modelBuilder.Entity<T>().HasQueryFilter(e => e.TenantId == CurrentTenantId && !e.IsDeleted);
    }

    // Both SaveChanges() and SaveChangesAsync(CancellationToken) delegate to these
    // "acceptAllChangesOnSuccess" overloads in the base DbContext, so overriding them here
    // guarantees audit/tenant/soft-delete rules run on every persistence path exactly once.
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditAndTenantRules();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAuditAndTenantRules();
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// Stamps audit fields, converts deletes into soft-deletes, and stamps TenantId on new
    /// tenant entities. Runs regardless of whether SaveChanges was called synchronously.
    /// </summary>
    private void ApplyAuditAndTenantRules()
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var userId = _currentUserService.UserId?.ToString();

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.CreatedBy = userId;
                    break;

                case EntityState.Modified:
                    entry.Entity.ModifiedAt = DateTime.UtcNow;
                    entry.Entity.ModifiedBy = userId;
                    break;

                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = DateTime.UtcNow;
                    entry.Entity.DeletedBy = userId;
                    break;
            }
        }

        foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
        {
            if (entry.State == EntityState.Added && tenantId.HasValue)
            {
                entry.Entity.TenantId = tenantId.Value;
            }
        }
    }
}
