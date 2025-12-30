using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using SMS.Application.Interfaces;

namespace SMS.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for ApplicationDbContext to support EF Core migrations
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../SMS.API"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        optionsBuilder.UseNpgsql(connectionString, 
            b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));

        return new ApplicationDbContext(
            optionsBuilder.Options,
            new DesignTimeTenantService(),
            new DesignTimeCurrentUserService());
    }
}

/// <summary>
/// Dummy tenant service for design-time operations
/// </summary>
internal class DesignTimeTenantService : ITenantService
{
    public Guid? GetCurrentTenantId() => null;
    public Task<string?> GetCurrentTenantNameAsync() => Task.FromResult<string?>(null);
    public void SetCurrentTenant(Guid tenantId) { }
}

/// <summary>
/// Dummy current user service for design-time operations
/// </summary>
internal class DesignTimeCurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public string? Email => null;
    public Guid? TenantId => null;
    public bool IsAuthenticated => false;
    public IEnumerable<string> Roles => [];
}
