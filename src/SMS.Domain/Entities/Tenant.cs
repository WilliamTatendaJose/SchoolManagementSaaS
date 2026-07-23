using SMS.Domain.Common;
using SMS.Domain.Enums;

namespace SMS.Domain.Entities;

/// <summary>
/// Represents a school (tenant) in the multi-tenant SaaS system
/// </summary>
public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    /// <summary>School logo as a data URI (data:image/...;base64,...) so it embeds in PDFs
    /// and renders in the UI without an external image host or S3 round-trip.</summary>
    public string? Logo { get; set; }
    /// <summary>Primary brand colour (#RRGGBB) used for headers/accents on branded PDFs.</summary>
    public string? PrimaryColor { get; set; }
    /// <summary>Secondary/accent brand colour (#RRGGBB).</summary>
    public string? AccentColor { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; } = "Zimbabwe";
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public TenantStatus Status { get; set; } = TenantStatus.Pending;
    public string? TimeZone { get; set; } = "Africa/Harare";
    public string? Currency { get; set; } = "USD";
    
    // Subscription details
    public SubscriptionPlan SubscriptionPlan { get; set; } = SubscriptionPlan.Basic;
    public DateTime? SubscriptionStartDate { get; set; }
    public DateTime? SubscriptionEndDate { get; set; }
    public int MaxStudents { get; set; } = 100;
    public int SmsCredits { get; set; } = 0;
    
    // Feature flags
    public bool HasLmsModule { get; set; }
    public bool HasTransportModule { get; set; }
    public bool HasHostelModule { get; set; }
    public bool HasLibraryModule { get; set; }
    
    // Navigation properties
    public virtual ICollection<User> Users { get; set; } = [];
    public virtual ICollection<Student> Students { get; set; } = [];
    public virtual ICollection<AcademicYear> AcademicYears { get; set; } = [];
    public virtual ICollection<Class> Classes { get; set; } = [];
    public virtual ICollection<Subject> Subjects { get; set; } = [];
}
