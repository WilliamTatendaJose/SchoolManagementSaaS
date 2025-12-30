using SMS.Domain.Common;
using SMS.Domain.Enums;

namespace SMS.Domain.Entities;

/// <summary>
/// Represents a student's attendance record
/// </summary>
public class Attendance : TenantEntity
{
    public Guid StudentId { get; set; }
    public Guid ClassId { get; set; }
    public Guid? SubjectId { get; set; }
    public Guid? TimetableSlotId { get; set; }
    public DateTime Date { get; set; }
    public AttendanceStatus Status { get; set; }
    public string? Reason { get; set; }
    public TimeOnly? TimeIn { get; set; }
    public TimeOnly? TimeOut { get; set; }
    public Guid? MarkedById { get; set; }
    public bool SmsNotificationSent { get; set; }
    
    // Navigation properties
    public virtual Student Student { get; set; } = null!;
    public virtual Class Class { get; set; } = null!;
    public virtual Subject? Subject { get; set; }
    public virtual TimetableSlot? TimetableSlot { get; set; }
    public virtual Staff? MarkedBy { get; set; }
}
