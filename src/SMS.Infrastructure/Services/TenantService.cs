using SMS.Application.Interfaces;

namespace SMS.Infrastructure.Services;

/// <summary>
/// Service for managing current tenant context
/// </summary>
public class TenantService : ITenantService
{
    private Guid? _currentTenantId;

    public Guid? GetCurrentTenantId() => _currentTenantId;

    public Task<string?> GetCurrentTenantNameAsync()
    {
        // This would typically query the database
        return Task.FromResult<string?>(null);
    }

    public void SetCurrentTenant(Guid tenantId)
    {
        _currentTenantId = tenantId;
    }
}
