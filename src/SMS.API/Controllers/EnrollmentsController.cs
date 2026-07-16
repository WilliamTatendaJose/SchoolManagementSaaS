using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common.Security;
using SMS.Application.Features.Enrollments.Commands;
using SMS.Application.Features.Enrollments.Queries;
using SMS.Infrastructure.Authorization;

namespace SMS.API.Controllers;

/// <summary>
/// API endpoints for student enrollment management
/// </summary>
[Authorize]
public class EnrollmentsController : BaseApiController
{
    /// <summary>
    /// Get paginated list of enrollments with optional filters
    /// </summary>
    [HttpGet]
    [RequirePermission(Permissions.EnrollmentsView)]
    public async Task<IActionResult> GetEnrollments([FromQuery] GetEnrollmentsQuery query)
    {
        var result = await Mediator.Send(query);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(result.Data);
    }

    /// <summary>
    /// Get the enrollment history for a single student
    /// </summary>
    [HttpGet("students/{studentId:guid}")]
    [RequirePermission(Permissions.EnrollmentsView)]
    public async Task<IActionResult> GetStudentEnrollments(Guid studentId)
    {
        var result = await Mediator.Send(new GetStudentEnrollmentsQuery(studentId));

        if (!result.IsSuccess)
            return NotFound(result.Error);

        return Ok(result.Data);
    }

    /// <summary>
    /// Enroll a student into a class for an academic year
    /// </summary>
    [HttpPost]
    [RequirePermission(Permissions.EnrollmentsManage)]
    public async Task<IActionResult> EnrollStudent([FromBody] EnrollStudentCommand command)
    {
        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(new { Id = result.Data });
    }

    /// <summary>
    /// Transfer a student's active enrollment to a different class/stream
    /// </summary>
    [HttpPost("transfer")]
    [RequirePermission(Permissions.EnrollmentsManage)]
    public async Task<IActionResult> TransferStudent([FromBody] TransferStudentCommand command)
    {
        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return NoContent();
    }

    /// <summary>
    /// Withdraw (deactivate) an enrollment
    /// </summary>
    [HttpPost("{id:guid}/withdraw")]
    [RequirePermission(Permissions.EnrollmentsManage)]
    public async Task<IActionResult> WithdrawEnrollment(Guid id)
    {
        var result = await Mediator.Send(new WithdrawEnrollmentCommand(id));

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return NoContent();
    }

    /// <summary>
    /// Bulk-promote students into a target class for a new academic year
    /// </summary>
    [HttpPost("promote")]
    [RequirePermission(Permissions.EnrollmentsPromote)]
    public async Task<IActionResult> PromoteStudents([FromBody] PromoteStudentsCommand command)
    {
        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(result.Data);
    }
}
