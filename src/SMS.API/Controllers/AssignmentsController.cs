using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common.Security;
using SMS.Application.Features.Lms.Commands;
using SMS.Application.Features.Lms.Queries;
using SMS.Infrastructure.Authorization;

namespace SMS.API.Controllers;

/// <summary>
/// Homework/assignments (LMS-lite): set assignments with an optional file attachment,
/// collect student submissions, and grade them.
/// </summary>
[Authorize]
public class AssignmentsController : BaseApiController
{
    [HttpGet]
    [RequirePermission(Permissions.AssignmentsView)]
    public async Task<IActionResult> GetAssignments([FromQuery] GetAssignmentsQuery query)
    {
        var result = await Mediator.Send(query);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpPost]
    [RequirePermission(Permissions.AssignmentsManage)]
    public async Task<IActionResult> CreateAssignment([FromForm] CreateAssignmentRequest request)
    {
        var (fileName, contentType, content) = await ReadAttachmentAsync(request.Attachment);

        var command = new CreateAssignmentCommand
        {
            ClassId = request.ClassId,
            SubjectId = request.SubjectId,
            AcademicTermId = request.AcademicTermId,
            Title = request.Title,
            Description = request.Description,
            DueDate = request.DueDate,
            AttachmentFileName = fileName,
            AttachmentContentType = contentType,
            AttachmentContent = content
        };

        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(new { Id = result.Data }) : BadRequest(result.Error);
    }

    [HttpGet("{assignmentId:guid}/submissions")]
    [RequirePermission(Permissions.AssignmentsView)]
    public async Task<IActionResult> GetSubmissions(Guid assignmentId)
    {
        var result = await Mediator.Send(new GetAssignmentSubmissionsQuery(assignmentId));
        return result.IsSuccess ? Ok(result.Data) : NotFound(result.Error);
    }

    [HttpPost("{assignmentId:guid}/submissions")]
    [RequirePermission(Permissions.AssignmentsManage)]
    public async Task<IActionResult> RecordSubmission(Guid assignmentId, [FromForm] RecordSubmissionRequest request)
    {
        var (fileName, contentType, content) = await ReadAttachmentAsync(request.Attachment);

        var command = new RecordAssignmentSubmissionCommand
        {
            AssignmentId = assignmentId,
            StudentId = request.StudentId,
            Comment = request.Comment,
            AttachmentFileName = fileName,
            AttachmentContentType = contentType,
            AttachmentContent = content
        };

        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(new { Id = result.Data }) : BadRequest(result.Error);
    }

    [HttpPost("submissions/{submissionId:guid}/grade")]
    [RequirePermission(Permissions.AssignmentsManage)]
    public async Task<IActionResult> GradeSubmission(Guid submissionId, [FromBody] GradeAssignmentSubmissionCommand command)
    {
        if (submissionId != command.SubmissionId)
            return BadRequest("ID mismatch");

        var result = await Mediator.Send(command);
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    [HttpGet("students/{studentId:guid}")]
    [RequirePermission(Permissions.AssignmentsView)]
    public async Task<IActionResult> GetStudentAssignments(Guid studentId)
    {
        var result = await Mediator.Send(new GetStudentAssignmentsQuery(studentId));
        return result.IsSuccess ? Ok(result.Data) : NotFound(result.Error);
    }

    private static async Task<(string? FileName, string? ContentType, byte[]? Content)> ReadAttachmentAsync(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return (null, null, null);
        }

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        return (file.FileName, file.ContentType, stream.ToArray());
    }
}

public class CreateAssignmentRequest
{
    public Guid ClassId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid AcademicTermId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DueDate { get; set; }
    public IFormFile? Attachment { get; set; }
}

public class RecordSubmissionRequest
{
    public Guid StudentId { get; set; }
    public string? Comment { get; set; }
    public IFormFile? Attachment { get; set; }
}
