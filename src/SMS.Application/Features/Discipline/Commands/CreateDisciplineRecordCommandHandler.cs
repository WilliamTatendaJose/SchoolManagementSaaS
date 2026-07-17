using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Messaging;
using SMS.Application.Common.Models;
using SMS.Application.Features.Notifications;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;

namespace SMS.Application.Features.Discipline.Commands;

public class CreateDisciplineRecordCommandHandler : IRequestHandler<CreateDisciplineRecordCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IEnumerable<IMessageChannel> _channels;
    private readonly ICurrentUserService _currentUser;

    public CreateDisciplineRecordCommandHandler(
        IApplicationDbContext context,
        IEnumerable<IMessageChannel> channels,
        ICurrentUserService currentUser)
    {
        _context = context;
        _channels = channels;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(CreateDisciplineRecordCommand request, CancellationToken cancellationToken)
    {
        var student = await _context.Students
            .Include(s => s.CurrentClass)
            .FirstOrDefaultAsync(s => s.Id == request.StudentId, cancellationToken);

        if (student == null)
        {
            return Result<Guid>.Failure("Student not found");
        }

        var reportedByStaffId = await _context.Staff
            .Where(s => s.UserId == _currentUser.UserId)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var record = new DisciplineRecord
        {
            StudentId = request.StudentId,
            IncidentDate = request.IncidentDate,
            IncidentType = request.IncidentType,
            Description = request.Description,
            ActionTaken = request.ActionTaken,
            DemeritsAwarded = request.DemeritsAwarded,
            MeritsAwarded = request.MeritsAwarded,
            ReportedById = reportedByStaffId
        };

        _context.DisciplineRecords.Add(record);

        if (request.NotifyGuardian)
        {
            await NotifyGuardianAsync(student, request, record, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(record.Id);
    }

    private async Task NotifyGuardianAsync(
        Student student, CreateDisciplineRecordCommand request, DisciplineRecord record, CancellationToken cancellationToken)
    {
        var channel = _channels.FirstOrDefault(c => c.Channel == MessageChannels.Sms);
        if (channel == null || _currentUser.UserId is not { } userId)
        {
            return;
        }

        var recipients = await RecipientResolver.ResolveForStudentsAsync(_context, [student.Id], cancellationToken);
        if (recipients.Count == 0)
        {
            return;
        }

        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == _currentUser.TenantId, cancellationToken);
        var schoolName = tenant?.Name ?? "School";
        var content = MessageTemplates.DisciplineNotice(student.FullName, request.IncidentType, request.IncidentDate, schoolName);

        var message = new Message
        {
            Subject = "Discipline notice",
            Content = content,
            MessageType = MessageTypes.Discipline,
            Channel = MessageChannels.Sms,
            RecipientType = "Guardian",
            CreatedByUserId = userId,
            Status = MessageStatuses.Draft
        };
        _context.Messages.Add(message);

        var targets = recipients.Select(r => (r, content)).ToList();
        await MessageDispatcher.DispatchAsync(channel, message, targets, cancellationToken);

        record.GuardianNotified = message.DeliveredCount > 0;
        record.NotificationDate = DateTime.UtcNow;
    }
}
