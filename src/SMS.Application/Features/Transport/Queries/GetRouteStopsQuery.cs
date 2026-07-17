using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Transport.Queries;

/// <summary>Lists a route's stops in sequence order.</summary>
public record GetRouteStopsQuery(Guid TransportRouteId) : IRequest<Result<List<RouteStopDto>>>;

public record RouteStopDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int SequenceNumber { get; init; }
    public TimeSpan? PickupTime { get; init; }
    public TimeSpan? DropoffTime { get; init; }
    public int RiderCount { get; init; }
}
