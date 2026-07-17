using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Transport.Queries;

public record GetTransportRoutesQuery : IRequest<Result<List<TransportRouteDto>>>;

public record TransportRouteDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? VehicleRegistration { get; init; }
    public Guid? DriverId { get; init; }
    public string? DriverName { get; init; }
    public int Capacity { get; init; }
    public bool IsActive { get; init; }
    public int StopCount { get; init; }
    public int Riders { get; init; }
    public int AvailableSeats { get; init; }
}
