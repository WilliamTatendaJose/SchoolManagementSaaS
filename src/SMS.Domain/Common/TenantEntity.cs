namespace SMS.Domain.Common;

/// <summary>
/// Base entity for tenant-scoped entities with automatic tenant isolation
/// </summary>
public abstract class TenantEntity : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
}
