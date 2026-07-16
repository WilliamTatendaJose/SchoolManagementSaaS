using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common.Security;
using SMS.Application.Features.Guardians.Commands;
using SMS.Application.Features.Guardians.Queries;
using SMS.Infrastructure.Authorization;

namespace SMS.API.Controllers;

/// <summary>
/// API endpoints for guardian management
/// </summary>
[Authorize]
public class GuardiansController : BaseApiController
{
    /// <summary>
    /// Get paginated list of guardians
    /// </summary>
    [HttpGet]
    [RequirePermission(Permissions.GuardiansView)]
    public async Task<IActionResult> GetGuardians([FromQuery] GetGuardiansQuery query)
    {
        var result = await Mediator.Send(query);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(result.Data);
    }

    /// <summary>
    /// Get guardian by ID, including linked students
    /// </summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.GuardiansView)]
    public async Task<IActionResult> GetGuardian(Guid id)
    {
        var result = await Mediator.Send(new GetGuardianByIdQuery(id));

        if (!result.IsSuccess)
            return NotFound(result.Error);

        return Ok(result.Data);
    }

    /// <summary>
    /// Create a new guardian, optionally linking to a student
    /// </summary>
    [HttpPost]
    [RequirePermission(Permissions.GuardiansCreate)]
    public async Task<IActionResult> CreateGuardian([FromBody] CreateGuardianCommand command)
    {
        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return CreatedAtAction(nameof(GetGuardian), new { id = result.Data }, new { Id = result.Data });
    }

    /// <summary>
    /// Update an existing guardian
    /// </summary>
    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.GuardiansEdit)]
    public async Task<IActionResult> UpdateGuardian(Guid id, [FromBody] UpdateGuardianCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch");

        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return NoContent();
    }

    /// <summary>
    /// Delete a guardian and remove any student links
    /// </summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission(Permissions.GuardiansDelete)]
    public async Task<IActionResult> DeleteGuardian(Guid id)
    {
        var result = await Mediator.Send(new DeleteGuardianCommand(id));

        if (!result.IsSuccess)
            return NotFound(result.Error);

        return NoContent();
    }

    /// <summary>
    /// Link an existing guardian to an existing student
    /// </summary>
    [HttpPost("{id:guid}/students")]
    [RequirePermission(Permissions.GuardiansEdit)]
    public async Task<IActionResult> LinkStudent(Guid id, [FromBody] LinkGuardianToStudentCommand command)
    {
        if (id != command.GuardianId)
            return BadRequest("ID mismatch");

        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(new { Id = result.Data });
    }

    /// <summary>
    /// Remove the link between a guardian and a student
    /// </summary>
    [HttpDelete("{id:guid}/students/{studentId:guid}")]
    [RequirePermission(Permissions.GuardiansEdit)]
    public async Task<IActionResult> UnlinkStudent(Guid id, Guid studentId)
    {
        var result = await Mediator.Send(new UnlinkGuardianFromStudentCommand(id, studentId));

        if (!result.IsSuccess)
            return NotFound(result.Error);

        return NoContent();
    }
}
