using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common.Security;
using SMS.Application.Features.Timetable.Commands;
using SMS.Application.Features.Timetable.Queries;
using SMS.Infrastructure.Authorization;

namespace SMS.API.Controllers;

/// <summary>
/// Timetable management: classrooms and lesson slots with clash detection.
/// </summary>
[Authorize]
public class TimetableController : BaseApiController
{
    // Classrooms

    [HttpGet("classrooms")]
    [RequirePermission(Permissions.TimetableView)]
    public async Task<IActionResult> GetClassrooms([FromQuery] GetClassroomsQuery query)
    {
        var result = await Mediator.Send(query);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpPost("classrooms")]
    [RequirePermission(Permissions.TimetableManage)]
    public async Task<IActionResult> CreateClassroom([FromBody] CreateClassroomCommand command)
    {
        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(new { Id = result.Data }) : BadRequest(result.Error);
    }

    [HttpPut("classrooms/{id:guid}")]
    [RequirePermission(Permissions.TimetableManage)]
    public async Task<IActionResult> UpdateClassroom(Guid id, [FromBody] UpdateClassroomCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch");

        var result = await Mediator.Send(command);
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    // Timetable slots

    [HttpGet]
    [RequirePermission(Permissions.TimetableView)]
    public async Task<IActionResult> GetTimetable([FromQuery] GetTimetableQuery query)
    {
        var result = await Mediator.Send(query);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpPost("slots")]
    [RequirePermission(Permissions.TimetableManage)]
    public async Task<IActionResult> CreateSlot([FromBody] CreateTimetableSlotCommand command)
    {
        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(new { Id = result.Data }) : BadRequest(result.Error);
    }

    [HttpPut("slots/{id:guid}")]
    [RequirePermission(Permissions.TimetableManage)]
    public async Task<IActionResult> UpdateSlot(Guid id, [FromBody] UpdateTimetableSlotCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch");

        var result = await Mediator.Send(command);
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    [HttpDelete("slots/{id:guid}")]
    [RequirePermission(Permissions.TimetableManage)]
    public async Task<IActionResult> DeleteSlot(Guid id)
    {
        var result = await Mediator.Send(new DeleteTimetableSlotCommand(id));
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }
}
