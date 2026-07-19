using SMS.Domain.Common;

namespace SMS.Domain.Entities;

/// <summary>
/// Represents a message recipient
/// </summary>
public class MessageRecipient : TenantEntity
{
    public Guid MessageId { get; set; }
    public string RecipientPhone { get; set; } = string.Empty;
    public string? RecipientEmail { get; set; }
    public string? RecipientName { get; set; }
    public Guid? StudentId { get; set; }
    public Guid? GuardianId { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? FailureReason { get; set; }

    /// <summary>
    /// The provider's id for the sent message (e.g. a WhatsApp <c>wamid</c>), used to
    /// correlate asynchronous delivery-status webhooks back to this recipient.
    /// </summary>
    public string? ProviderMessageId { get; set; }

    // Navigation properties
    public virtual Message Message { get; set; } = null!;
    public virtual Student? Student { get; set; }
    public virtual Guardian? Guardian { get; set; }
}
