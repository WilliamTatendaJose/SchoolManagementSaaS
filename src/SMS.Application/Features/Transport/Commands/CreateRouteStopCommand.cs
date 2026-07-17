using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Transport.Commands;

public record CreateRouteStopCommand : IRequest<Result<Guid>>
{
    public Guid TransportRouteId { get; init; }
    public string Name { get; init; } = string.Empty;
    public int SequenceNumber { get; init; }
    public TimeSpan? PickupTime { get; init; }
    public TimeSpan? DropoffTime { get; init; }
}
