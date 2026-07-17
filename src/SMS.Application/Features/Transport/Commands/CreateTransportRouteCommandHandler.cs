using MediatR;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;

namespace SMS.Application.Features.Transport.Commands;

public class CreateTransportRouteCommandHandler : IRequestHandler<CreateTransportRouteCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateTransportRouteCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateTransportRouteCommand request, CancellationToken cancellationToken)
    {
        var route = new TransportRoute
        {
            Name = request.Name,
            VehicleRegistration = request.VehicleRegistration,
            DriverId = request.DriverId,
            Capacity = request.Capacity,
            IsActive = true
        };

        _context.TransportRoutes.Add(route);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(route.Id);
    }
}
