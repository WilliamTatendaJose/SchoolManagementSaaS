using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common.Security;
using SMS.Application.Features.Lms.Commands;
using SMS.Application.Features.Lms.Queries;
using SMS.Infrastructure.Authorization;

namespace SMS.API.Controllers;

/// <summary>
/// Course materials library (LMS-lite): teachers/staff publish notes, worksheets and links
/// to a class for a subject; students and their guardians read them through the portal.
/// Reuses the Assignments permissions (no separate permission is seeded).
/// </summary>
[Authorize]
public class CourseMaterialsController : BaseApiController
{
    [HttpGet]
    [RequirePermission(Permissions.AssignmentsView)]
    public async Task<IActionResult> GetMaterials([FromQuery] GetCourseMaterialsQuery query)
    {
        var result = await Mediator.Send(query);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpPost]
    [RequirePermission(Permissions.AssignmentsManage)]
    public async Task<IActionResult> CreateMaterial([FromForm] CreateCourseMaterialRequest request)
    {
        var (fileName, contentType, content) = await ReadAttachmentAsync(request.Attachment);

        var command = new CreateCourseMaterialCommand
        {
            ClassId = request.ClassId,
            SubjectId = request.SubjectId,
            AcademicTermId = request.AcademicTermId,
            Title = request.Title,
            Description = request.Description,
            Url = request.Url,
            AttachmentFileName = fileName,
            AttachmentContentType = contentType,
            AttachmentContent = content
        };

        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(new { Id = result.Data }) : BadRequest(result.Error);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(Permissions.AssignmentsManage)]
    public async Task<IActionResult> DeleteMaterial(Guid id)
    {
        var result = await Mediator.Send(new DeleteCourseMaterialCommand(id));
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
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

public class CreateCourseMaterialRequest
{
    public Guid ClassId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid? AcademicTermId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Url { get; set; }
    public IFormFile? Attachment { get; set; }
}
