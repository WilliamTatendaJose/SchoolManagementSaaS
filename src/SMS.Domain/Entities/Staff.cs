using SMS.Domain.Common;

namespace SMS.Domain.Entities;

/// <summary>
/// Represents a staff member (teacher, admin, etc.)
/// </summary>
public class Staff : TenantEntity
{
    public Guid UserId { get; set; }
    public string StaffNumber { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? JobTitle { get; set; }
    public DateTime? DateOfJoining { get; set; }
    public string? Qualifications { get; set; }
    public string? Specialization { get; set; }
    public bool IsTeacher { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual ICollection<TeacherSubject> Subjects { get; set; } = [];
    public virtual ICollection<Class> ClassesAsTeacher { get; set; } = [];
    public virtual ICollection<TimetableSlot> TimetableSlots { get; set; } = [];
    public virtual ICollection<LeaveRequest> LeaveRequests { get; set; } = [];
}
