using MediatR;
using SMS.Application.Common.Messaging;
using SMS.Application.Common.Models;
using SMS.Application.Features.Notifications.Commands;

namespace SMS.Application.Features.Lms.Commands;

/// <summary>
/// Messages the guardians of every student in the class who has not yet submitted the
/// assignment, over the chosen channel. Reuses the shared notification pipeline.
/// </summary>
public record RemindNonSubmittersCommand : IRequest<Result<MessageDispatchResultDto>>
{
    public Guid AssignmentId { get; init; }
    public string Channel { get; init; } = MessageChannels.Sms;
}
