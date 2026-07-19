using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common.Security;
using SMS.Application.Features.Students.Commands;
using SMS.Application.Features.Students.Queries;
using SMS.Infrastructure.Authorization;

namespace SMS.API.Controllers;

/// <summary>
/// API endpoints for student management
/// </summary>
[Authorize]
public class StudentsController : BaseApiController
{
    /// <summary>
    /// Get paginated list of students
    /// </summary>
    [HttpGet]
    [RequirePermission(Permissions.StudentsView)]
    public async Task<IActionResult> GetStudents([FromQuery] GetStudentsQuery query)
    {
        var result = await Mediator.Send(query);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(result.Data);
    }

    /// <summary>
    /// Get student by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.StudentsView)]
    public async Task<IActionResult> GetStudent(Guid id)
    {
        var result = await Mediator.Send(new GetStudentByIdQuery(id));

        if (!result.IsSuccess)
            return NotFound(result.Error);

        return Ok(result.Data);
    }

    /// <summary>
    /// Create a new student
    /// </summary>
    [HttpPost]
    [RequirePermission(Permissions.StudentsCreate)]
    public async Task<IActionResult> CreateStudent([FromBody] CreateStudentCommand command)
    {
        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return CreatedAtAction(nameof(GetStudent), new { id = result.Data }, new { Id = result.Data });
    }

    /// <summary>
    /// Update an existing student
    /// </summary>
    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.StudentsEdit)]
    public async Task<IActionResult> UpdateStudent(Guid id, [FromBody] UpdateStudentCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch");

        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return NoContent();
    }

    /// <summary>
    /// Delete a student
    /// </summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission(Permissions.StudentsDelete)]
    public async Task<IActionResult> DeleteStudent(Guid id)
    {
        var result = await Mediator.Send(new DeleteStudentCommand(id));

        if (!result.IsSuccess)
            return NotFound(result.Error);

        return NoContent();
    }

    /// <summary>
    /// Validate (and optionally commit) a bulk student import parsed from a spreadsheet.
    /// Call with commit=false first to get a per-row validation report; once every row is
    /// valid, call again with commit=true to create the students/guardians/enrollments.
    /// </summary>
    [HttpPost("import")]
    [RequirePermission(Permissions.StudentsImport)]
    public async Task<IActionResult> ImportStudents([FromBody] ImportStudentsCommand command)
    {
        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(result.Data);
    }
}
