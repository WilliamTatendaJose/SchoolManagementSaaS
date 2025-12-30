namespace SMS.Domain.Common;

/// <summary>
/// Interface for entities that belong to a specific tenant
/// </summary>
public interface ITenantEntity
{
    Guid TenantId { get; set; }
}
