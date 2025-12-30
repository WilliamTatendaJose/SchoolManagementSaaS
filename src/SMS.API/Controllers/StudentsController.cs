using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Features.Students.Commands;
using SMS.Application.Features.Students.Queries;

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
    public async Task<IActionResult> DeleteStudent(Guid id)
    {
        var result = await Mediator.Send(new DeleteStudentCommand(id));
        
        if (!result.IsSuccess)
            return NotFound(result.Error);
            
        return NoContent();
    }
}
