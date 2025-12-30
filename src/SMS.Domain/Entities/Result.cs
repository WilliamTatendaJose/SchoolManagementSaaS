using SMS.Domain.Common;

namespace SMS.Domain.Entities;

/// <summary>
/// Represents a student's result for an assessment
/// </summary>
public class Result : TenantEntity
{
    public Guid StudentId { get; set; }
    public Guid AssessmentId { get; set; }
    public decimal Score { get; set; }
    public string? Grade { get; set; }
    public string? Comment { get; set; }
    public bool IsModerated { get; set; }
    public Guid? ModeratedById { get; set; }
    public DateTime? ModeratedAt { get; set; }
    
    // Navigation properties
    public virtual Student Student { get; set; } = null!;
    public virtual Assessment Assessment { get; set; } = null!;
    public virtual Staff? ModeratedBy { get; set; }
}
