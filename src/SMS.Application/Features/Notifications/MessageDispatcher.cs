using SMS.Application.Common.Messaging;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;

namespace SMS.Application.Features.Notifications;

/// <summary>
/// Sends the resolved recipients' content over a channel, recording a MessageRecipient row
/// and delivery status for each, and rolling the counts up onto the Message.
/// </summary>
internal static class MessageDispatcher
{
    public static async Task DispatchAsync(
        IMessageChannel channel,
        Message message,
        IReadOnlyList<(ResolvedRecipient Recipient, string Content)> targets,
        CancellationToken cancellationToken)
    {
        message.TotalRecipients = targets.Count;
        message.Status = MessageStatuses.Sending;

        var delivered = 0;
        var failed = 0;

        foreach (var (recipient, content) in targets)
        {
            var messageRecipient = new MessageRecipient
            {
                RecipientPhone = recipient.Phone,
                RecipientName = recipient.Name,
                StudentId = recipient.StudentId,
                GuardianId = recipient.GuardianId,
                Status = RecipientStatuses.Pending
            };
            message.Recipients.Add(messageRecipient);

            var result = await channel.SendAsync(recipient.Phone, content, cancellationToken);
            if (result.Success)
            {
                messageRecipient.Status = RecipientStatuses.Delivered;
                messageRecipient.DeliveredAt = DateTime.UtcNow;
                delivered++;
            }
            else
            {
                messageRecipient.Status = RecipientStatuses.Failed;
                messageRecipient.FailureReason = result.FailureReason;
                failed++;
            }
        }

        message.DeliveredCount = delivered;
        message.FailedCount = failed;
        message.SentAt = DateTime.UtcNow;
        message.Status = MessageStatuses.Sent;
    }

    /// <summary>
    /// Records the recipients as Pending without sending — used to queue a message for later
    /// dispatch by the outbox processor. The stored Message.Content is what will be sent.
    /// </summary>
    public static void Materialize(
        Message message,
        IReadOnlyList<ResolvedRecipient> recipients)
    {
        message.TotalRecipients = recipients.Count;
        message.Status = MessageStatuses.Queued;

        foreach (var recipient in recipients)
        {
            message.Recipients.Add(new MessageRecipient
            {
                RecipientPhone = recipient.Phone,
                RecipientName = recipient.Name,
                StudentId = recipient.StudentId,
                GuardianId = recipient.GuardianId,
                Status = RecipientStatuses.Pending
            });
        }
    }

    /// <summary>
    /// Dispatches a queued message's Pending recipients (sending Message.Content to each) and
    /// finalizes the counts/status. Safe to retry: only Pending recipients are (re)sent.
    /// </summary>
    public static async Task DispatchPendingAsync(
        IMessageChannel channel,
        Message message,
        CancellationToken cancellationToken)
    {
        message.Status = MessageStatuses.Sending;

        foreach (var recipient in message.Recipients.Where(r => r.Status == RecipientStatuses.Pending))
        {
            var result = await channel.SendAsync(recipient.RecipientPhone, message.Content, cancellationToken);
            if (result.Success)
            {
                recipient.Status = RecipientStatuses.Delivered;
                recipient.DeliveredAt = DateTime.UtcNow;
            }
            else
            {
                recipient.Status = RecipientStatuses.Failed;
                recipient.FailureReason = result.FailureReason;
            }
        }

        message.DeliveredCount = message.Recipients.Count(r => r.Status == RecipientStatuses.Delivered);
        message.FailedCount = message.Recipients.Count(r => r.Status == RecipientStatuses.Failed);
        message.SentAt = DateTime.UtcNow;
        message.Status = MessageStatuses.Sent;
    }
}
