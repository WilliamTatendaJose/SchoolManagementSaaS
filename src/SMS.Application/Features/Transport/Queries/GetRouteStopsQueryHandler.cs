using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Transport.Queries;

public class GetRouteStopsQueryHandler : IRequestHandler<GetRouteStopsQuery, Result<List<RouteStopDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetRouteStopsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<RouteStopDto>>> Handle(GetRouteStopsQuery request, CancellationToken cancellationToken)
    {
        if (!await _context.TransportRoutes.AnyAsync(r => r.Id == request.TransportRouteId, cancellationToken))
        {
            return Result<List<RouteStopDto>>.Failure("Transport route not found");
        }

        var stops = await _context.RouteStops
            .AsNoTracking()
            .Where(s => s.TransportRouteId == request.TransportRouteId)
            .OrderBy(s => s.SequenceNumber)
            .Select(s => new RouteStopDto
            {
                Id = s.Id,
                Name = s.Name,
                SequenceNumber = s.SequenceNumber,
                PickupTime = s.PickupTime,
                DropoffTime = s.DropoffTime,
                RiderCount = _context.Students.Count(st => st.RouteStopId == s.Id)
            })
            .ToListAsync(cancellationToken);

        return Result<List<RouteStopDto>>.Success(stops);
    }
}
