using SMS.Domain.Common;

namespace SMS.Domain.Entities;

/// <summary>
/// Represents an assessment (exam, test, assignment)
/// </summary>
public class Assessment : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid SubjectId { get; set; }
    public Guid ClassId { get; set; }
    public Guid AcademicTermId { get; set; }
    public string AssessmentType { get; set; } = string.Empty;
    public decimal MaxScore { get; set; }
    public decimal WeightPercentage { get; set; }
    public DateTime? Date { get; set; }
    public bool IsPublished { get; set; }
    
    // Navigation properties
    public virtual Subject Subject { get; set; } = null!;
    public virtual Class Class { get; set; } = null!;
    public virtual AcademicTerm AcademicTerm { get; set; } = null!;
    public virtual ICollection<Result> Results { get; set; } = [];
}
