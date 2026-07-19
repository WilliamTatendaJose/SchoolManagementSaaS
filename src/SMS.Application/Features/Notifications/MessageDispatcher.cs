using SMS.Application.Common.Messaging;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;

namespace SMS.Application.Features.Notifications;

/// <summary>
/// One recipient's rendered content for a dispatch. <see cref="TemplateParams"/> is set
/// only for business-initiated notifications (fee reminders, discipline notices,
/// assignment reminders) where a template send makes sense; when set, a channel that
/// supports templates (WhatsApp) sends the approved template for the message's type
/// instead of freeform text - required outside Meta's 24h reply window.
/// </summary>
internal sealed record DispatchTarget(ResolvedRecipient Recipient, string Content, IReadOnlyList<string>? TemplateParams = null);

/// <summary>
/// Sends the resolved recipients' content over a channel, recording a MessageRecipient row
/// and delivery status for each, and rolling the counts up onto the Message.
/// </summary>
internal static class MessageDispatcher
{
    public static async Task DispatchAsync(
        IMessageChannel channel,
        Message message,
        IReadOnlyList<DispatchTarget> targets,
        CancellationToken cancellationToken)
    {
        message.TotalRecipients = targets.Count;
        message.Status = MessageStatuses.Sending;

        foreach (var target in targets)
        {
            var messageRecipient = new MessageRecipient
            {
                RecipientPhone = target.Recipient.Phone,
                RecipientName = target.Recipient.Name,
                StudentId = target.Recipient.StudentId,
                GuardianId = target.Recipient.GuardianId,
                Status = RecipientStatuses.Pending
            };
            message.Recipients.Add(messageRecipient);

            var result = target.TemplateParams is { } templateParams
                ? await channel.SendTemplateAsync(target.Recipient.Phone, message.MessageType, templateParams, target.Content, cancellationToken)
                : await channel.SendAsync(target.Recipient.Phone, target.Content, cancellationToken);
            ApplySendResult(messageRecipient, channel, result);
        }

        RollUpCounts(message);
        message.SentAt = DateTime.UtcNow;
        message.Status = MessageStatuses.Sent;
    }

    /// <summary>
    /// Applies a send result to a recipient. For a channel with async delivery receipts a
    /// successful send is only "Sent" (a later webhook confirms delivery); otherwise it is
    /// treated as delivered immediately.
    /// </summary>
    private static void ApplySendResult(MessageRecipient recipient, IMessageChannel channel, MessageDeliveryResult result)
    {
        if (result.Success)
        {
            recipient.ProviderMessageId = result.ProviderMessageId;
            if (channel.SupportsDeliveryReceipts)
            {
                recipient.Status = RecipientStatuses.Sent;
            }
            else
            {
                recipient.Status = RecipientStatuses.Delivered;
                recipient.DeliveredAt = DateTime.UtcNow;
            }
        }
        else
        {
            recipient.Status = RecipientStatuses.Failed;
            recipient.FailureReason = result.FailureReason;
        }
    }

    /// <summary>Recomputes the message's delivered/failed rollup from its recipients.</summary>
    internal static void RollUpCounts(Message message)
    {
        message.DeliveredCount = message.Recipients.Count(r => RecipientStatuses.IsConfirmedDelivered(r.Status));
        message.FailedCount = message.Recipients.Count(r => r.Status == RecipientStatuses.Failed);
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
            ApplySendResult(recipient, channel, result);
        }

        RollUpCounts(message);
        message.SentAt = DateTime.UtcNow;
        message.Status = MessageStatuses.Sent;
    }
}
