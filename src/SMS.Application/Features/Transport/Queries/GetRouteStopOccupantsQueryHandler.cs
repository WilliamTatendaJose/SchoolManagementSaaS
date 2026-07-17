using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Transport.Queries;

public class GetRouteStopOccupantsQueryHandler : IRequestHandler<GetRouteStopOccupantsQuery, Result<List<RouteStopOccupantDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetRouteStopOccupantsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<RouteStopOccupantDto>>> Handle(GetRouteStopOccupantsQuery request, CancellationToken cancellationToken)
    {
        if (!await _context.RouteStops.AnyAsync(s => s.Id == request.RouteStopId, cancellationToken))
        {
            return Result<List<RouteStopOccupantDto>>.Failure("Route stop not found");
        }

        var occupants = await _context.Students
            .AsNoTracking()
            .Include(s => s.CurrentClass)
            .Where(s => s.RouteStopId == request.RouteStopId)
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .Select(s => new RouteStopOccupantDto
            {
                StudentId = s.Id,
                StudentNumber = s.StudentNumber,
                FullName = s.FirstName + " " + s.LastName,
                ClassName = s.CurrentClass != null ? s.CurrentClass.Name : null
            })
            .ToListAsync(cancellationToken);

        return Result<List<RouteStopOccupantDto>>.Success(occupants);
    }
}
