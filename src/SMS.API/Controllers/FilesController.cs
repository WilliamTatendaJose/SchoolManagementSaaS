using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Features.Files.Queries;

namespace SMS.API.Controllers;

/// <summary>
/// Serves files stored in the database (assignment attachments, submissions, course
/// materials). Authenticated and tenant-scoped: a caller can only fetch files belonging to
/// their own tenant. Any authenticated user may download - the URLs are only ever handed
/// out by already-authorized/ownership-scoped queries.
/// </summary>
[Authorize]
public class FilesController : BaseApiController
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Download(Guid id)
    {
        var result = await Mediator.Send(new GetStoredFileQuery(id));
        if (!result.IsSuccess || result.Data is null)
        {
            return NotFound();
        }

        return File(result.Data.Content, result.Data.ContentType, result.Data.FileName);
    }
}
