using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common.Security;
using SMS.Application.Features.Transport.Commands;
using SMS.Application.Features.Transport.Queries;
using SMS.Infrastructure.Authorization;

namespace SMS.API.Controllers;

/// <summary>
/// School transport: bus routes, stops, and student-to-stop allocation.
/// </summary>
[Authorize]
public class TransportController : BaseApiController
{
    [HttpGet("routes")]
    [RequirePermission(Permissions.TransportView)]
    public async Task<IActionResult> GetRoutes()
    {
        var result = await Mediator.Send(new GetTransportRoutesQuery());
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpPost("routes")]
    [RequirePermission(Permissions.TransportManage)]
    public async Task<IActionResult> CreateRoute([FromBody] CreateTransportRouteCommand command)
    {
        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(new { Id = result.Data }) : BadRequest(result.Error);
    }

    [HttpGet("routes/{routeId:guid}/stops")]
    [RequirePermission(Permissions.TransportView)]
    public async Task<IActionResult> GetRouteStops(Guid routeId)
    {
        var result = await Mediator.Send(new GetRouteStopsQuery(routeId));
        return result.IsSuccess ? Ok(result.Data) : NotFound(result.Error);
    }

    [HttpPost("stops")]
    [RequirePermission(Permissions.TransportManage)]
    public async Task<IActionResult> CreateStop([FromBody] CreateRouteStopCommand command)
    {
        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(new { Id = result.Data }) : BadRequest(result.Error);
    }

    [HttpGet("stops/{stopId:guid}/occupants")]
    [RequirePermission(Permissions.TransportView)]
    public async Task<IActionResult> GetStopOccupants(Guid stopId)
    {
        var result = await Mediator.Send(new GetRouteStopOccupantsQuery(stopId));
        return result.IsSuccess ? Ok(result.Data) : NotFound(result.Error);
    }

    [HttpPost("stops/assign")]
    [RequirePermission(Permissions.TransportManage)]
    public async Task<IActionResult> AssignStudent([FromBody] AssignStudentToRouteStopCommand command)
    {
        var result = await Mediator.Send(command);
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }
}
