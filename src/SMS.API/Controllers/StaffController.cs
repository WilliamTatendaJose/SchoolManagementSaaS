using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common.Security;
using SMS.Application.Features.Staff.Commands;
using SMS.Application.Features.Staff.Queries;
using SMS.Infrastructure.Authorization;

namespace SMS.API.Controllers;

/// <summary>
/// Staff/HR management: staff profiles and teacher subject allocation.
/// </summary>
[Authorize]
public class StaffController : BaseApiController
{
    [HttpGet]
    [RequirePermission(Permissions.StaffView)]
    public async Task<IActionResult> GetStaff([FromQuery] GetStaffQuery query)
    {
        var result = await Mediator.Send(query);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.StaffView)]
    public async Task<IActionResult> GetStaffMember(Guid id)
    {
        var result = await Mediator.Send(new GetStaffByIdQuery(id));
        return result.IsSuccess ? Ok(result.Data) : NotFound(result.Error);
    }

    [HttpPost]
    [RequirePermission(Permissions.StaffCreate)]
    public async Task<IActionResult> CreateStaff([FromBody] CreateStaffCommand command)
    {
        var result = await Mediator.Send(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetStaffMember), new { id = result.Data }, new { Id = result.Data })
            : BadRequest(result.Error);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.StaffEdit)]
    public async Task<IActionResult> UpdateStaff(Guid id, [FromBody] UpdateStaffCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch");

        var result = await Mediator.Send(command);
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(Permissions.StaffDelete)]
    public async Task<IActionResult> DeleteStaff(Guid id)
    {
        var result = await Mediator.Send(new DeleteStaffCommand(id));
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    [HttpPost("{id:guid}/subjects")]
    [RequirePermission(Permissions.StaffEdit)]
    public async Task<IActionResult> AssignSubject(Guid id, [FromBody] AssignSubjectToTeacherCommand command)
    {
        if (id != command.StaffId)
            return BadRequest("ID mismatch");

        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(new { Id = result.Data }) : BadRequest(result.Error);
    }

    [HttpDelete("{id:guid}/subjects/{subjectId:guid}")]
    [RequirePermission(Permissions.StaffEdit)]
    public async Task<IActionResult> RemoveSubject(Guid id, Guid subjectId)
    {
        var result = await Mediator.Send(new RemoveSubjectFromTeacherCommand(id, subjectId));
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }
}
