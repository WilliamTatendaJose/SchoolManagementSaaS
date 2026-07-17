using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Transport.Queries;

/// <summary>Lists the students currently assigned to a route stop.</summary>
public record GetRouteStopOccupantsQuery(Guid RouteStopId) : IRequest<Result<List<RouteStopOccupantDto>>>;

public record RouteStopOccupantDto
{
    public Guid StudentId { get; init; }
    public string StudentNumber { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? ClassName { get; init; }
}
