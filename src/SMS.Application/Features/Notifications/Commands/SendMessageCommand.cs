using MediatR;
using SMS.Application.Common.Messaging;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Notifications.Commands;

public enum MessageAudience
{
    SpecificStudents,
    Class,
    AllActiveStudents
}

/// <summary>
/// Sends the same message to the guardians of a chosen audience over a channel.
/// </summary>
public record SendMessageCommand : IRequest<Result<MessageDispatchResultDto>>
{
    public string Channel { get; init; } = MessageChannels.Sms;
    public string Subject { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public MessageAudience Audience { get; init; }
    public List<Guid>? StudentIds { get; init; }
    public Guid? ClassId { get; init; }
}

public record MessageDispatchResultDto
{
    public Guid MessageId { get; init; }
    public int TotalRecipients { get; init; }
    public int Delivered { get; init; }
    public int Failed { get; init; }
}
