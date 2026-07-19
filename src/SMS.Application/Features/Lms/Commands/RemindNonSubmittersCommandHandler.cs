using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Messaging;
using SMS.Application.Common.Models;
using SMS.Application.Features.Notifications;
using SMS.Application.Features.Notifications.Commands;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;
using SMS.Domain.Enums;

namespace SMS.Application.Features.Lms.Commands;

public class RemindNonSubmittersCommandHandler : IRequestHandler<RemindNonSubmittersCommand, Result<MessageDispatchResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IEnumerable<IMessageChannel> _channels;
    private readonly ICurrentUserService _currentUser;

    public RemindNonSubmittersCommandHandler(
        IApplicationDbContext context,
        IEnumerable<IMessageChannel> channels,
        ICurrentUserService currentUser)
    {
        _context = context;
        _channels = channels;
        _currentUser = currentUser;
    }

    public async Task<Result<MessageDispatchResultDto>> Handle(RemindNonSubmittersCommand request, CancellationToken cancellationToken)
    {
        var channel = _channels.FirstOrDefault(c => c.Channel == request.Channel);
        if (channel == null)
        {
            return Result<MessageDispatchResultDto>.Failure($"Unsupported channel '{request.Channel}'");
        }

        if (_currentUser.UserId is not { } userId)
        {
            return Result<MessageDispatchResultDto>.Failure("No authenticated user");
        }

        var assignment = await _context.Assignments
            .Include(a => a.Subject)
            .FirstOrDefaultAsync(a => a.Id == request.AssignmentId, cancellationToken);
        if (assignment == null)
        {
            return Result<MessageDispatchResultDto>.Failure("Assignment not found");
        }

        var rosterIds = await _context.Students
            .Where(s => s.CurrentClassId == assignment.ClassId && s.Status == StudentStatus.Active)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var submittedIds = (await _context.AssignmentSubmissions
            .Where(s => s.AssignmentId == request.AssignmentId)
            .Select(s => s.StudentId)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        var nonSubmitterIds = rosterIds.Where(id => !submittedIds.Contains(id)).ToHashSet();
        if (nonSubmitterIds.Count == 0)
        {
            return Result<MessageDispatchResultDto>.Failure("Every student in the class has already submitted");
        }

        var nameByStudent = await _context.Students
            .Where(s => nonSubmitterIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.FirstName + " " + s.LastName, cancellationToken);

        var recipients = await RecipientResolver.ResolveForStudentsAsync(_context, nonSubmitterIds, cancellationToken);
        if (recipients.Count == 0)
        {
            return Result<MessageDispatchResultDto>.Failure("No non-submitters have a contactable guardian");
        }

        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == _currentUser.TenantId, cancellationToken);
        var schoolName = tenant?.Name ?? "School";

        var message = new Message
        {
            Subject = "Assignment reminder",
            Content = $"Assignment reminders for non-submitters of \"{assignment.Title}\" ({schoolName}).",
            MessageType = MessageTypes.AssignmentReminder,
            Channel = request.Channel,
            RecipientType = "Guardian",
            ClassId = assignment.ClassId,
            CreatedByUserId = userId,
            Status = MessageStatuses.Draft
        };
        _context.Messages.Add(message);

        var targets = recipients
            .Select(r =>
            {
                var studentName = nameByStudent.GetValueOrDefault(r.StudentId, "your child");
                return new DispatchTarget(
                    r,
                    MessageTemplates.AssignmentReminder(studentName, assignment.Title, assignment.Subject.Name, assignment.DueDate, schoolName),
                    MessageTemplates.TemplateParams.AssignmentReminder(studentName, assignment.Subject.Name, assignment.Title, assignment.DueDate));
            })
            .ToList();

        await MessageDispatcher.DispatchAsync(channel, message, targets, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return Result<MessageDispatchResultDto>.Success(new MessageDispatchResultDto
        {
            MessageId = message.Id,
            TotalRecipients = message.TotalRecipients,
            Delivered = message.DeliveredCount,
            Failed = message.FailedCount
        });
    }
}
