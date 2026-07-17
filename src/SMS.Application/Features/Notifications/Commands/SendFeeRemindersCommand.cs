using MediatR;
using SMS.Application.Common.Messaging;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Notifications.Commands;

/// <summary>
/// Sends a personalised fee reminder to the guardian of every student with an outstanding
/// balance, optionally scoped to a single class.
/// </summary>
public record SendFeeRemindersCommand : IRequest<Result<MessageDispatchResultDto>>
{
    public string Channel { get; init; } = MessageChannels.Sms;
    public Guid? ClassId { get; init; }
}
