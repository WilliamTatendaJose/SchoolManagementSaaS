namespace SMS.Application.Interfaces;

/// <summary>
/// Interface for accessing current tenant information
/// </summary>
public interface ITenantService
{
    Guid? GetCurrentTenantId();
    Task<string?> GetCurrentTenantNameAsync();
    void SetCurrentTenant(Guid tenantId);
}
