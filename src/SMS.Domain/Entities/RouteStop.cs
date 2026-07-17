using SMS.Domain.Common;

namespace SMS.Domain.Entities;

/// <summary>
/// A single pick-up/drop-off point along a <see cref="TransportRoute"/>.
/// </summary>
public class RouteStop : TenantEntity
{
    public Guid TransportRouteId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SequenceNumber { get; set; }
    public TimeSpan? PickupTime { get; set; }
    public TimeSpan? DropoffTime { get; set; }

    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual TransportRoute TransportRoute { get; set; } = null!;
    public virtual ICollection<Student> Students { get; set; } = [];
}
