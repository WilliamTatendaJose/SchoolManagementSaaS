using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Messaging;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;

namespace SMS.Application.Features.Notifications.Commands;

public class SendFeeRemindersCommandHandler : IRequestHandler<SendFeeRemindersCommand, Result<MessageDispatchResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IEnumerable<IMessageChannel> _channels;
    private readonly ICurrentUserService _currentUser;

    public SendFeeRemindersCommandHandler(
        IApplicationDbContext context,
        IEnumerable<IMessageChannel> channels,
        ICurrentUserService currentUser)
    {
        _context = context;
        _channels = channels;
        _currentUser = currentUser;
    }

    public async Task<Result<MessageDispatchResultDto>> Handle(SendFeeRemindersCommand request, CancellationToken cancellationToken)
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

        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == _currentUser.TenantId, cancellationToken);
        var currency = tenant?.Currency ?? "USD";
        var schoolName = tenant?.Name ?? "School";

        // Sum invoice balances per student (computed in memory to avoid provider-specific
        // decimal arithmetic in SQL).
        var studentScope = _context.Students.AsQueryable();
        if (request.ClassId is { } classId)
        {
            studentScope = studentScope.Where(s => s.CurrentClassId == classId);
        }

        var scopedStudents = await studentScope
            .Select(s => new { s.Id, Name = s.FirstName + " " + s.LastName })
            .ToListAsync(cancellationToken);

        var scopedIds = scopedStudents.Select(s => s.Id).ToHashSet();

        var invoiceRows = await _context.Invoices
            .Where(i => scopedIds.Contains(i.StudentId))
            .Select(i => new { i.StudentId, i.TotalAmount, i.DiscountAmount, i.PaidAmount })
            .ToListAsync(cancellationToken);

        var balanceByStudent = invoiceRows
            .GroupBy(i => i.StudentId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.TotalAmount - x.DiscountAmount - x.PaidAmount));

        var nameByStudent = scopedStudents.ToDictionary(s => s.Id, s => s.Name);

        var defaulterIds = balanceByStudent
            .Where(kv => kv.Value > 0)
            .Select(kv => kv.Key)
            .ToHashSet();

        if (defaulterIds.Count == 0)
        {
            return Result<MessageDispatchResultDto>.Failure("No students have an outstanding balance");
        }

        var recipients = await RecipientResolver.ResolveForStudentsAsync(_context, defaulterIds, cancellationToken);
        if (recipients.Count == 0)
        {
            return Result<MessageDispatchResultDto>.Failure("No defaulters have a contactable guardian");
        }

        var message = new Message
        {
            Subject = "Fee reminder",
            Content = $"Personalised fee reminders for students with outstanding balances ({schoolName}).",
            MessageType = MessageTypes.FeeReminder,
            Channel = request.Channel,
            RecipientType = "Defaulters",
            ClassId = request.ClassId,
            CreatedByUserId = userId,
            Status = MessageStatuses.Draft
        };
        _context.Messages.Add(message);

        var targets = recipients
            .Select(r =>
            {
                var studentName = nameByStudent.GetValueOrDefault(r.StudentId, "your child");
                var balance = balanceByStudent[r.StudentId];
                return new DispatchTarget(
                    r,
                    MessageTemplates.FeeReminder(studentName, balance, currency, schoolName),
                    MessageTemplates.TemplateParams.FeeReminder(studentName, balance, currency));
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
