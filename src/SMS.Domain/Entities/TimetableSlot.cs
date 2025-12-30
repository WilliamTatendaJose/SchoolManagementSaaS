using SMS.Domain.Common;

namespace SMS.Domain.Entities;

/// <summary>
/// Represents a timetable slot
/// </summary>
public class TimetableSlot : TenantEntity
{
    public Guid ClassId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid TeacherId { get; set; }
    public Guid? ClassroomId { get; set; }
    public Guid AcademicTermId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    
    // Navigation properties
    public virtual Class Class { get; set; } = null!;
    public virtual Subject Subject { get; set; } = null!;
    public virtual Staff Teacher { get; set; } = null!;
    public virtual Classroom? Classroom { get; set; }
    public virtual AcademicTerm AcademicTerm { get; set; } = null!;
}
