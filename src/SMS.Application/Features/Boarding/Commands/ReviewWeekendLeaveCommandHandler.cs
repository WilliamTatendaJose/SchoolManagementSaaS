using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Messaging;
using SMS.Application.Common.Models;
using SMS.Application.Features.Notifications;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;
using Result = SMS.Application.Common.Models.Result;

namespace SMS.Application.Features.Boarding.Commands;

public class ReviewWeekendLeaveCommandHandler : IRequestHandler<ReviewWeekendLeaveCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IEnumerable<IMessageChannel> _channels;
    private readonly ICurrentUserService _currentUser;

    public ReviewWeekendLeaveCommandHandler(
        IApplicationDbContext context,
        IEnumerable<IMessageChannel> channels,
        ICurrentUserService currentUser)
    {
        _context = context;
        _channels = channels;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(ReviewWeekendLeaveCommand request, CancellationToken cancellationToken)
    {
        var leave = await _context.WeekendLeaves
            .Include(l => l.Student)
            .FirstOrDefaultAsync(l => l.Id == request.LeaveId, cancellationToken);

        if (leave == null)
        {
            return Result.Failure("Leave request not found");
        }

        if (leave.Status != WeekendLeaveStatuses.Pending)
        {
            return Result.Failure($"Only pending requests can be reviewed (current status: {leave.Status})");
        }

        leave.Status = request.Approve ? WeekendLeaveStatuses.Approved : WeekendLeaveStatuses.Rejected;
        leave.ReviewNote = request.ReviewNote;
        leave.AuthorizedById = await _context.Staff
            .Where(s => s.UserId == _currentUser.UserId)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (request.Approve && request.NotifyGuardian)
        {
            await NotifyGuardianAsync(leave, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private async Task NotifyGuardianAsync(WeekendLeave leave, CancellationToken cancellationToken)
    {
        var channel = _channels.FirstOrDefault(c => c.Channel == MessageChannels.Sms);
        if (channel == null || _currentUser.UserId is not { } userId)
        {
            return;
        }

        var recipients = await RecipientResolver.ResolveForStudentsAsync(_context, [leave.StudentId], cancellationToken);
        if (recipients.Count == 0)
        {
            return;
        }

        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == _currentUser.TenantId, cancellationToken);
        var schoolName = tenant?.Name ?? "School";
        var content = MessageTemplates.WeekendLeaveApproved(leave.Student.FullName, leave.DepartureDate, leave.ExpectedReturnDate, schoolName);

        var message = new Message
        {
            Subject = "Weekend leave approved",
            Content = content,
            MessageType = MessageTypes.BoardingLeave,
            Channel = MessageChannels.Sms,
            RecipientType = "Guardian",
            CreatedByUserId = userId,
            Status = MessageStatuses.Draft
        };
        _context.Messages.Add(message);

        var targets = recipients.Select(r => (r, content)).ToList();
        await MessageDispatcher.DispatchAsync(channel, message, targets, cancellationToken);

        leave.GuardianNotified = message.DeliveredCount > 0;
    }
}
