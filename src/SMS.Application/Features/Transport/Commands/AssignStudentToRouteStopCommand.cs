using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Transport.Commands;

/// <summary>Assigns (or, with a null stop, un-assigns) a student to a route stop.</summary>
public record AssignStudentToRouteStopCommand : IRequest<Result>
{
    public Guid StudentId { get; init; }
    public Guid? RouteStopId { get; init; }
}
