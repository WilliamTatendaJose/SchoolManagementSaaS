using SMS.Domain.Common;

namespace SMS.Domain.Entities;

/// <summary>
/// A student's borrowing of one copy of a <see cref="Book"/>.
/// </summary>
public class BookLoan : TenantEntity
{
    public Guid BookId { get; set; }
    public Guid StudentId { get; set; }
    public DateTime BorrowedDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ReturnedDate { get; set; }

    /// <summary>Borrowed, Returned or Lost.</summary>
    public string Status { get; set; } = "Borrowed";

    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual Book Book { get; set; } = null!;
    public virtual Student Student { get; set; } = null!;
}
