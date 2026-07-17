using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common.Security;
using SMS.Application.Features.Boarding.Commands;
using SMS.Application.Features.Boarding.Queries;
using SMS.Application.Features.Hostel.Commands;
using SMS.Application.Features.Hostel.Queries;
using SMS.Infrastructure.Authorization;

namespace SMS.API.Controllers;

/// <summary>
/// Boarding management: dormitories, houses, and student allocation.
/// </summary>
[Authorize]
public class HostelController : BaseApiController
{
    // Dormitories

    [HttpGet("dormitories")]
    [RequirePermission(Permissions.HostelView)]
    public async Task<IActionResult> GetDormitories()
    {
        var result = await Mediator.Send(new GetDormitoriesQuery());
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpGet("dormitories/{id:guid}/occupants")]
    [RequirePermission(Permissions.HostelView)]
    public async Task<IActionResult> GetDormitoryOccupants(Guid id)
    {
        var result = await Mediator.Send(new GetDormitoryOccupantsQuery(id));
        return result.IsSuccess ? Ok(result.Data) : NotFound(result.Error);
    }

    [HttpPost("dormitories")]
    [RequirePermission(Permissions.HostelManage)]
    public async Task<IActionResult> CreateDormitory([FromBody] CreateDormitoryCommand command)
    {
        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(new { Id = result.Data }) : BadRequest(result.Error);
    }

    [HttpPost("dormitories/assign")]
    [RequirePermission(Permissions.HostelManage)]
    public async Task<IActionResult> AssignDormitory([FromBody] AssignStudentToDormitoryCommand command)
    {
        var result = await Mediator.Send(command);
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    // Houses

    [HttpGet("houses")]
    [RequirePermission(Permissions.HostelView)]
    public async Task<IActionResult> GetHouses()
    {
        var result = await Mediator.Send(new GetHousesQuery());
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpPost("houses")]
    [RequirePermission(Permissions.HostelManage)]
    public async Task<IActionResult> CreateHouse([FromBody] CreateHouseCommand command)
    {
        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(new { Id = result.Data }) : BadRequest(result.Error);
    }

    [HttpPost("houses/assign")]
    [RequirePermission(Permissions.HostelManage)]
    public async Task<IActionResult> AssignHouse([FromBody] AssignStudentToHouseCommand command)
    {
        var result = await Mediator.Send(command);
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    // Weekend-leave register

    [HttpGet("leaves")]
    [RequirePermission(Permissions.HostelView)]
    public async Task<IActionResult> GetWeekendLeaves([FromQuery] GetWeekendLeavesQuery query)
    {
        var result = await Mediator.Send(query);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpPost("leaves")]
    [RequirePermission(Permissions.HostelManage)]
    public async Task<IActionResult> RequestWeekendLeave([FromBody] RequestWeekendLeaveCommand command)
    {
        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(new { Id = result.Data }) : BadRequest(result.Error);
    }

    [HttpPost("leaves/{id:guid}/review")]
    [RequirePermission(Permissions.HostelManage)]
    public async Task<IActionResult> ReviewWeekendLeave(Guid id, [FromBody] ReviewWeekendLeaveCommand command)
    {
        if (id != command.LeaveId)
            return BadRequest("ID mismatch");

        var result = await Mediator.Send(command);
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    [HttpPost("leaves/{id:guid}/depart")]
    [RequirePermission(Permissions.HostelManage)]
    public async Task<IActionResult> RecordLeaveDeparture(Guid id, [FromBody] RecordLeaveDepartureCommand command)
    {
        if (id != command.LeaveId)
            return BadRequest("ID mismatch");

        var result = await Mediator.Send(command);
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    [HttpPost("leaves/{id:guid}/return")]
    [RequirePermission(Permissions.HostelManage)]
    public async Task<IActionResult> RecordLeaveReturn(Guid id, [FromBody] RecordLeaveReturnCommand command)
    {
        if (id != command.LeaveId)
            return BadRequest("ID mismatch");

        var result = await Mediator.Send(command);
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }
}
