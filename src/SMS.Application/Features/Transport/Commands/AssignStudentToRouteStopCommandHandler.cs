using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Transport.Commands;

public class AssignStudentToRouteStopCommandHandler : IRequestHandler<AssignStudentToRouteStopCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public AssignStudentToRouteStopCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(AssignStudentToRouteStopCommand request, CancellationToken cancellationToken)
    {
        var student = await _context.Students.FirstOrDefaultAsync(s => s.Id == request.StudentId, cancellationToken);
        if (student == null)
        {
            return Result.Failure("Student not found");
        }

        if (request.RouteStopId is null)
        {
            student.RouteStopId = null;
            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        var stop = await _context.RouteStops
            .FirstOrDefaultAsync(s => s.Id == request.RouteStopId.Value, cancellationToken);

        if (stop == null)
        {
            return Result.Failure("Route stop not found");
        }

        if (student.RouteStopId == stop.Id)
        {
            return Result.Success();
        }

        var route = await _context.TransportRoutes
            .FirstOrDefaultAsync(r => r.Id == stop.TransportRouteId, cancellationToken);

        if (route == null || !route.IsActive)
        {
            return Result.Failure("Route is not active");
        }

        // Capacity is enforced against the vehicle (the whole route), not the individual stop.
        var stopIdsOnRoute = await _context.RouteStops
            .Where(s => s.TransportRouteId == route.Id)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var riders = await _context.Students
            .CountAsync(s => s.RouteStopId != null && stopIdsOnRoute.Contains(s.RouteStopId.Value), cancellationToken);

        if (riders >= route.Capacity)
        {
            return Result.Failure("The route is at full capacity");
        }

        student.RouteStopId = stop.Id;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
