using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Notifications.Queries;

public class GetMessageByIdQueryHandler : IRequestHandler<GetMessageByIdQuery, Result<MessageDetailDto>>
{
    private readonly IApplicationDbContext _context;

    public GetMessageByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<MessageDetailDto>> Handle(GetMessageByIdQuery request, CancellationToken cancellationToken)
    {
        var message = await _context.Messages
            .AsNoTracking()
            .Include(m => m.Recipients)
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);

        if (message == null)
        {
            return Result<MessageDetailDto>.Failure("Message not found");
        }

        var dto = new MessageDetailDto
        {
            Id = message.Id,
            Subject = message.Subject,
            Content = message.Content,
            Channel = message.Channel,
            MessageType = message.MessageType,
            Status = message.Status,
            TotalRecipients = message.TotalRecipients,
            DeliveredCount = message.DeliveredCount,
            FailedCount = message.FailedCount,
            SentAt = message.SentAt,
            CreatedAt = message.CreatedAt,
            Recipients = message.Recipients.Select(r => new MessageRecipientDto
            {
                RecipientName = r.RecipientName,
                RecipientPhone = r.RecipientPhone,
                StudentId = r.StudentId,
                Status = r.Status,
                DeliveredAt = r.DeliveredAt,
                ReadAt = r.ReadAt,
                FailureReason = r.FailureReason
            }).ToList()
        };

        return Result<MessageDetailDto>.Success(dto);
    }
}
