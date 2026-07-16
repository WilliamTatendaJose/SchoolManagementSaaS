using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Interfaces;
using SMS.Infrastructure.Persistence;
using SMS.Infrastructure.Services;

namespace SMS.Infrastructure.Tests;

/// <summary>
/// Test double for the current user - the tenant filter/stamping only needs a user id.
/// </summary>
internal sealed class TestCurrentUserService : ICurrentUserService
{
    public Guid? UserId { get; set; }
    public string? Email { get; set; }
    public Guid? TenantId { get; set; }
    public IEnumerable<string> Roles { get; set; } = [];
    public bool IsAuthenticated => UserId.HasValue;
}

/// <summary>
/// Hosts a private in-memory SQLite database shared across many short-lived
/// <see cref="ApplicationDbContext"/> instances - mirroring how the request pipeline
/// creates a fresh scoped context per request. All contexts share the one
/// <see cref="TenantService"/> instance that EF's cached model captures in the tenant
/// filter, so switching the active tenant is honoured by each newly created context.
/// </summary>
internal sealed class TenantTestContext : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public TenantService TenantService { get; }
    public TestCurrentUserService CurrentUser { get; }

    public TenantTestContext()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        TenantService = new TenantService();
        CurrentUser = new TestCurrentUserService();

        using var db = CreateDbContext();
        db.Database.EnsureCreated();
    }

    /// <summary>
    /// Creates a fresh context over the shared database and tenant/user services.
    /// </summary>
    public ApplicationDbContext CreateDbContext() => new(_options, TenantService, CurrentUser);

    /// <summary>
    /// Switch the active tenant for contexts created afterwards.
    /// </summary>
    public void UseTenant(Guid tenantId)
    {
        TenantService.SetCurrentTenant(tenantId);
        CurrentUser.TenantId = tenantId;
    }

    public void Dispose() => _connection.Dispose();
}
