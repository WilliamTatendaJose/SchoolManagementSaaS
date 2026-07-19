using MediatR;
using SMS.Application.Common.Messaging;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Discipline.Commands;

/// <summary>
/// Records a discipline incident (or merit) for a student, optionally notifying the guardian.
/// </summary>
public record CreateDisciplineRecordCommand : IRequest<Result<Guid>>
{
    public Guid StudentId { get; init; }
    public DateTime IncidentDate { get; init; }
    public string IncidentType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? ActionTaken { get; init; }
    public int? DemeritsAwarded { get; init; }
    public int? MeritsAwarded { get; init; }
    public bool NotifyGuardian { get; init; }
    /// <summary>Channel for the guardian notification, when requested. Ignored when
    /// NotifyGuardian is false.</summary>
    public string Channel { get; init; } = MessageChannels.Sms;
}
