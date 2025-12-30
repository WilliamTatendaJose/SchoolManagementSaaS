using SMS.Domain.Common;

namespace SMS.Domain.Entities;

/// <summary>
/// Represents a classroom/physical room
/// </summary>
public class Classroom : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Building { get; set; }
    public int Capacity { get; set; }
    public bool HasProjector { get; set; }
    public bool HasWhiteboard { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual ICollection<Class> Classes { get; set; } = [];
    public virtual ICollection<TimetableSlot> TimetableSlots { get; set; } = [];
}
