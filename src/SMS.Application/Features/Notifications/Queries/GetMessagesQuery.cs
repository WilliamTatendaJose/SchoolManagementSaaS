using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Notifications.Queries;

/// <summary>
/// Query to list sent/queued messages with delivery counts.
/// </summary>
public record GetMessagesQuery : IRequest<Result<PaginatedList<MessageListDto>>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? Channel { get; init; }
    public string? MessageType { get; init; }
    public string? Status { get; init; }
}

public record MessageListDto
{
    public Guid Id { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string Channel { get; init; } = string.Empty;
    public string MessageType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int TotalRecipients { get; init; }
    public int DeliveredCount { get; init; }
    public int FailedCount { get; init; }
    public DateTime? SentAt { get; init; }
    public DateTime CreatedAt { get; init; }
}
