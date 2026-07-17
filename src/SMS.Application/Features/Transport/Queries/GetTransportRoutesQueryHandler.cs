using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Transport.Queries;

public class GetTransportRoutesQueryHandler : IRequestHandler<GetTransportRoutesQuery, Result<List<TransportRouteDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetTransportRoutesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<TransportRouteDto>>> Handle(GetTransportRoutesQuery request, CancellationToken cancellationToken)
    {
        var routes = await _context.TransportRoutes
            .AsNoTracking()
            .Include(r => r.Driver)
                .ThenInclude(d => d!.User)
            .OrderBy(r => r.Name)
            .Select(r => new
            {
                r.Id,
                r.Name,
                r.VehicleRegistration,
                r.DriverId,
                DriverName = r.Driver != null ? r.Driver.User.FirstName + " " + r.Driver.User.LastName : null,
                r.Capacity,
                r.IsActive,
                StopCount = r.Stops.Count,
                Riders = _context.Students.Count(s => s.RouteStop != null && s.RouteStop.TransportRouteId == r.Id)
            })
            .ToListAsync(cancellationToken);

        var result = routes
            .Select(r => new TransportRouteDto
            {
                Id = r.Id,
                Name = r.Name,
                VehicleRegistration = r.VehicleRegistration,
                DriverId = r.DriverId,
                DriverName = r.DriverName,
                Capacity = r.Capacity,
                IsActive = r.IsActive,
                StopCount = r.StopCount,
                Riders = r.Riders,
                AvailableSeats = Math.Max(0, r.Capacity - r.Riders)
            })
            .ToList();

        return Result<List<TransportRouteDto>>.Success(result);
    }
}
