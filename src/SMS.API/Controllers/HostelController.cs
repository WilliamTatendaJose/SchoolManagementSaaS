using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common.Security;
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
}
