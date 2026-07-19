using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Notifications.Queries;

public record GetMessageByIdQuery(Guid Id) : IRequest<Result<MessageDetailDto>>;

public record MessageDetailDto
{
    public Guid Id { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string Channel { get; init; } = string.Empty;
    public string MessageType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int TotalRecipients { get; init; }
    public int DeliveredCount { get; init; }
    public int FailedCount { get; init; }
    public DateTime? SentAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<MessageRecipientDto> Recipients { get; init; } = [];
}

public record MessageRecipientDto
{
    public string? RecipientName { get; init; }
    public string RecipientPhone { get; init; } = string.Empty;
    public Guid? StudentId { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime? DeliveredAt { get; init; }
    public DateTime? ReadAt { get; init; }
    public string? FailureReason { get; init; }
}
