using SMS.Domain.Common;

namespace SMS.Domain.Entities;

/// <summary>
/// Represents a discipline record/incident
/// </summary>
public class DisciplineRecord : TenantEntity
{
    public Guid StudentId { get; set; }
    public DateTime IncidentDate { get; set; }
    public string IncidentType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ActionTaken { get; set; }
    public int? DemeritsAwarded { get; set; }
    public int? MeritsAwarded { get; set; }
    public Guid? ReportedById { get; set; }
    public bool GuardianNotified { get; set; }
    public DateTime? NotificationDate { get; set; }
    public string? GuardianResponse { get; set; }
    
    // Navigation properties
    public virtual Student Student { get; set; } = null!;
    public virtual Staff? ReportedBy { get; set; }
}
