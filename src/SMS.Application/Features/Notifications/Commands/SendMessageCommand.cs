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

    /// <summary>
    /// One of <see cref="MessageAudience"/>'s names, as a string - matching this project's
    /// convention of never binding a real enum type on a request DTO (there's no
    /// JsonStringEnumConverter registered, so ASP.NET's default JSON binding only accepts
    /// enum ordinals, not names, which no API client should have to know about).
    /// </summary>
    public string Audience { get; init; } = string.Empty;
    public List<Guid>? StudentIds { get; init; }
    public Guid? ClassId { get; init; }

    /// <summary>When set to a future time, the message is queued and sent by the outbox processor.</summary>
    public DateTime? ScheduledAt { get; init; }
}

public record MessageDispatchResultDto
{
    public Guid MessageId { get; init; }
    public int TotalRecipients { get; init; }
    public int Delivered { get; init; }
    public int Failed { get; init; }
}
