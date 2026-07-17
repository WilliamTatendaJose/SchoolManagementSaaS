using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common.Security;
using SMS.Application.Features.Staff.Commands;
using SMS.Application.Features.Staff.Queries;
using SMS.Infrastructure.Authorization;

namespace SMS.API.Controllers;

/// <summary>
/// Staff leave requests and approval workflow.
/// </summary>
[Authorize]
public class LeaveController : BaseApiController
{
    [HttpGet]
    [RequirePermission(Permissions.StaffView)]
    public async Task<IActionResult> GetLeaveRequests([FromQuery] GetLeaveRequestsQuery query)
    {
        var result = await Mediator.Send(query);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpPost]
    [RequirePermission(Permissions.StaffView)]
    public async Task<IActionResult> CreateLeaveRequest([FromBody] CreateLeaveRequestCommand command)
    {
        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(new { Id = result.Data }) : BadRequest(result.Error);
    }

    [HttpPost("{id:guid}/review")]
    [RequirePermission(Permissions.LeaveApprove)]
    public async Task<IActionResult> ReviewLeaveRequest(Guid id, [FromBody] ReviewLeaveRequestCommand command)
    {
        if (id != command.LeaveRequestId)
            return BadRequest("ID mismatch");

        var result = await Mediator.Send(command);
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }
}
