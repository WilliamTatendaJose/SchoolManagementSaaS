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
}
