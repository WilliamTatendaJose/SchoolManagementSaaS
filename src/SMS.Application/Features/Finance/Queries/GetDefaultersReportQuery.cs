using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Finance.Queries;

/// <summary>
/// Lists students with an outstanding fees balance, with their primary guardian contact,
/// for follow-up. Optionally scoped to a class.
/// </summary>
public record GetDefaultersReportQuery : IRequest<Result<List<DefaulterDto>>>
{
    public Guid? ClassId { get; init; }
}

public record DefaulterDto
{
    public Guid StudentId { get; init; }
    public string StudentNumber { get; init; } = string.Empty;
    public string StudentName { get; init; } = string.Empty;
    public string? ClassName { get; init; }
    public string? GuardianName { get; init; }
    public string? GuardianPhone { get; init; }
    public decimal OutstandingBalance { get; init; }
}
