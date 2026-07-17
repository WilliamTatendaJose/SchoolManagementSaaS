using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Discipline.Queries;

public record GetDisciplineRecordsQuery : IRequest<Result<PaginatedList<DisciplineRecordDto>>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public Guid? StudentId { get; init; }
    public string? IncidentType { get; init; }
}

public record DisciplineRecordDto
{
    public Guid Id { get; init; }
    public Guid StudentId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public DateTime IncidentDate { get; init; }
    public string IncidentType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? ActionTaken { get; init; }
    public int? DemeritsAwarded { get; init; }
    public int? MeritsAwarded { get; init; }
    public bool GuardianNotified { get; init; }
    public DateTime? NotificationDate { get; init; }
}
