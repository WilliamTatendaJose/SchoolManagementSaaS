using SMS.Domain.Common;

namespace SMS.Domain.Entities;

/// <summary>
/// A library catalog entry. <see cref="TotalCopies"/> tracks how many physical copies the
/// school owns; availability is derived from active <see cref="BookLoan"/> records.
/// </summary>
public class Book : TenantEntity
{
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? Isbn { get; set; }
    public string? Category { get; set; }
    public int TotalCopies { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual ICollection<BookLoan> Loans { get; set; } = [];
}
