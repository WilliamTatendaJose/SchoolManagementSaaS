using SMS.Domain.Common;

namespace SMS.Domain.Entities;

/// <summary>
/// Represents a class/grade level
/// </summary>
public class Class : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public int Level { get; set; }
    public int Capacity { get; set; }
    public Guid? ClassTeacherId { get; set; }
    public Guid? ClassroomId { get; set; }
    
    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual Staff? ClassTeacher { get; set; }
    public virtual Classroom? Classroom { get; set; }
    public virtual ICollection<Stream> Streams { get; set; } = [];
    public virtual ICollection<Student> Students { get; set; } = [];
    public virtual ICollection<ClassSubject> Subjects { get; set; } = [];
    public virtual ICollection<FeeStructure> FeeStructures { get; set; } = [];
}
