using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Transport.Commands;

public record CreateTransportRouteCommand : IRequest<Result<Guid>>
{
    public string Name { get; init; } = string.Empty;
    public string? VehicleRegistration { get; init; }
    public Guid? DriverId { get; init; }
    public int Capacity { get; init; }
}
