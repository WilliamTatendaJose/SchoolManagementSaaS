using SMS.Domain.Common;

namespace SMS.Domain.Entities;

/// <summary>
/// Represents a leave request from staff
/// </summary>
public class LeaveRequest : TenantEntity
{
    public Guid StaffId { get; set; }
    public string LeaveType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = "Pending";
    public Guid? ApprovedById { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApproverComments { get; set; }
    
    public int NumberOfDays => (EndDate - StartDate).Days + 1;
    
    // Navigation properties
    public virtual Staff Staff { get; set; } = null!;
    public virtual Staff? ApprovedBy { get; set; }
}
