using SMS.Domain.Common;

namespace SMS.Domain.Entities;

/// <summary>
/// Represents a stream within a class (e.g., Form 1A, Form 1B)
/// </summary>
public class Stream : TenantEntity
{
    public Guid ClassId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    
    // Navigation properties
    public virtual Class Class { get; set; } = null!;
    public virtual ICollection<Enrollment> Enrollments { get; set; } = [];
}
