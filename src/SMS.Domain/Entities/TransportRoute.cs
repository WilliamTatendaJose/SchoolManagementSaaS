using SMS.Domain.Common;

namespace SMS.Domain.Entities;

/// <summary>
/// A school bus route: one vehicle serving an ordered list of stops.
/// </summary>
public class TransportRoute : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? VehicleRegistration { get; set; }
    public Guid? DriverId { get; set; }
    public int Capacity { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual Staff? Driver { get; set; }
    public virtual ICollection<RouteStop> Stops { get; set; } = [];
}
