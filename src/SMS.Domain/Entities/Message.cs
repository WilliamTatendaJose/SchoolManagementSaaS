using SMS.Domain.Common;

namespace SMS.Domain.Entities;

/// <summary>
/// Represents a communication message (SMS/Email)
/// </summary>
public class Message : TenantEntity
{
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string MessageType { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string? RecipientType { get; set; }
    public Guid? ClassId { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }
    public string Status { get; set; } = "Draft";
    public int TotalRecipients { get; set; }
    public int DeliveredCount { get; set; }
    public int FailedCount { get; set; }
    public Guid CreatedByUserId { get; set; }
    
    // Navigation properties
    public virtual Class? Class { get; set; }
    public virtual User CreatedByUser { get; set; } = null!;
    public virtual ICollection<MessageRecipient> Recipients { get; set; } = [];
}
