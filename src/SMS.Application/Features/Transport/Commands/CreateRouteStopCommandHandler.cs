using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;

namespace SMS.Application.Features.Transport.Commands;

public class CreateRouteStopCommandHandler : IRequestHandler<CreateRouteStopCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateRouteStopCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateRouteStopCommand request, CancellationToken cancellationToken)
    {
        var routeExists = await _context.TransportRoutes.AnyAsync(r => r.Id == request.TransportRouteId, cancellationToken);
        if (!routeExists)
        {
            return Result<Guid>.Failure("Transport route not found");
        }

        var sequenceTaken = await _context.RouteStops
            .AnyAsync(s => s.TransportRouteId == request.TransportRouteId && s.SequenceNumber == request.SequenceNumber, cancellationToken);
        if (sequenceTaken)
        {
            return Result<Guid>.Failure("A stop with this sequence number already exists on the route");
        }

        var stop = new RouteStop
        {
            TransportRouteId = request.TransportRouteId,
            Name = request.Name,
            SequenceNumber = request.SequenceNumber,
            PickupTime = request.PickupTime,
            DropoffTime = request.DropoffTime
        };

        _context.RouteStops.Add(stop);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(stop.Id);
    }
}
