using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Notifications.Queries;

public class GetMessagesQueryHandler : IRequestHandler<GetMessagesQuery, Result<PaginatedList<MessageListDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetMessagesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<MessageListDto>>> Handle(GetMessagesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Messages.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Channel))
        {
            query = query.Where(m => m.Channel == request.Channel);
        }

        if (!string.IsNullOrWhiteSpace(request.MessageType))
        {
            query = query.Where(m => m.MessageType == request.MessageType);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(m => m.Status == request.Status);
        }

        query = query.OrderByDescending(m => m.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var messages = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(m => new MessageListDto
            {
                Id = m.Id,
                Subject = m.Subject,
                Channel = m.Channel,
                MessageType = m.MessageType,
                Status = m.Status,
                TotalRecipients = m.TotalRecipients,
                DeliveredCount = m.DeliveredCount,
                FailedCount = m.FailedCount,
                SentAt = m.SentAt,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var paginatedList = new PaginatedList<MessageListDto>(messages, totalCount, request.PageNumber, request.PageSize);

        return Result<PaginatedList<MessageListDto>>.Success(paginatedList);
    }
}
