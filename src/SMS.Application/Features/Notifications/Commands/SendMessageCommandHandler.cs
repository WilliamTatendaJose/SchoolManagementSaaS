using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Messaging;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;
using SMS.Domain.Enums;

namespace SMS.Application.Features.Notifications.Commands;

public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, Result<MessageDispatchResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IEnumerable<IMessageChannel> _channels;
    private readonly ICurrentUserService _currentUser;

    public SendMessageCommandHandler(
        IApplicationDbContext context,
        IEnumerable<IMessageChannel> channels,
        ICurrentUserService currentUser)
    {
        _context = context;
        _channels = channels;
        _currentUser = currentUser;
    }

    public async Task<Result<MessageDispatchResultDto>> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<MessageAudience>(request.Audience, out var audience))
        {
            return Result<MessageDispatchResultDto>.Failure($"Invalid audience '{request.Audience}'");
        }

        var channel = _channels.FirstOrDefault(c => c.Channel == request.Channel);
        if (channel == null)
        {
            return Result<MessageDispatchResultDto>.Failure($"Unsupported channel '{request.Channel}'");
        }

        if (_currentUser.UserId is not { } userId)
        {
            return Result<MessageDispatchResultDto>.Failure("No authenticated user");
        }

        var studentIds = await ResolveStudentIdsAsync(request, audience, cancellationToken);
        if (studentIds.Count == 0)
        {
            return Result<MessageDispatchResultDto>.Failure("No students matched the selected audience");
        }

        var recipients = await RecipientResolver.ResolveForStudentsAsync(_context, studentIds, cancellationToken);
        if (recipients.Count == 0)
        {
            return Result<MessageDispatchResultDto>.Failure("No recipients with a contactable guardian were found");
        }

        var message = new Message
        {
            Subject = request.Subject,
            Content = request.Content,
            MessageType = MessageTypes.Announcement,
            Channel = request.Channel,
            RecipientType = audience.ToString(),
            ClassId = audience == MessageAudience.Class ? request.ClassId : null,
            ScheduledAt = request.ScheduledAt,
            CreatedByUserId = userId,
            Status = MessageStatuses.Draft
        };
        _context.Messages.Add(message);

        // Future-dated messages are queued for the outbox processor; others send now.
        if (request.ScheduledAt is { } scheduledAt && scheduledAt > DateTime.UtcNow)
        {
            MessageDispatcher.Materialize(message, recipients);
        }
        else
        {
            var targets = recipients.Select(r => new DispatchTarget(r, request.Content)).ToList();
            await MessageDispatcher.DispatchAsync(channel, message, targets, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result<MessageDispatchResultDto>.Success(new MessageDispatchResultDto
        {
            MessageId = message.Id,
            TotalRecipients = message.TotalRecipients,
            Delivered = message.DeliveredCount,
            Failed = message.FailedCount
        });
    }

    private async Task<List<Guid>> ResolveStudentIdsAsync(SendMessageCommand request, MessageAudience audience, CancellationToken cancellationToken)
    {
        switch (audience)
        {
            case MessageAudience.SpecificStudents:
                return request.StudentIds?.Distinct().ToList() ?? [];

            case MessageAudience.Class:
                if (request.ClassId is not { } classId)
                {
                    return [];
                }
                return await _context.Students
                    .Where(s => s.CurrentClassId == classId && s.Status == StudentStatus.Active)
                    .Select(s => s.Id)
                    .ToListAsync(cancellationToken);

            case MessageAudience.AllActiveStudents:
                return await _context.Students
                    .Where(s => s.Status == StudentStatus.Active)
                    .Select(s => s.Id)
                    .ToListAsync(cancellationToken);

            default:
                return [];
        }
    }
}
