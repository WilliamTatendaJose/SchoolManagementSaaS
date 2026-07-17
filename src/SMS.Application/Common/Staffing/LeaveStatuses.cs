namespace SMS.Application.Common.Staffing;

/// <summary>Leave request lifecycle statuses (stored on <c>LeaveRequest.Status</c>).</summary>
public static class LeaveStatuses
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Cancelled = "Cancelled";
}
