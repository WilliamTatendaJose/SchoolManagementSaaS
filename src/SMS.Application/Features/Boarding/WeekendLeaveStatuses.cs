namespace SMS.Application.Features.Boarding;

/// <summary>Lifecycle statuses for a <c>WeekendLeave</c> (stored on <c>WeekendLeave.Status</c>).</summary>
public static class WeekendLeaveStatuses
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Departed = "Departed";
    public const string Returned = "Returned";
}
