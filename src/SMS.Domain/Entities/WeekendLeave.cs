using SMS.Domain.Common;

namespace SMS.Domain.Entities;

/// <summary>
/// A boarding student's request to leave campus (typically for a weekend) and the
/// sign-out / sign-in record that tracks their departure and return.
/// </summary>
public class WeekendLeave : TenantEntity
{
    public Guid StudentId { get; set; }
    public DateTime DepartureDate { get; set; }
    public DateTime ExpectedReturnDate { get; set; }
    public string Destination { get; set; } = string.Empty;
    public string? Reason { get; set; }

    /// <summary>Pending, Approved, Rejected, Departed or Returned.</summary>
    public string Status { get; set; } = "Pending";

    public string? ReviewNote { get; set; }
    public Guid? AuthorizedById { get; set; }

    /// <summary>Name of the person who collected the student at sign-out.</summary>
    public string? CollectedBy { get; set; }
    public DateTime? ActualDepartureAt { get; set; }
    public DateTime? ActualReturnAt { get; set; }

    public bool GuardianNotified { get; set; }

    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual Student Student { get; set; } = null!;
    public virtual Staff? AuthorizedBy { get; set; }
}
