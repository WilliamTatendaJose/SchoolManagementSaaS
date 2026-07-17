using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Features.ParentPortal.Queries;

namespace SMS.API.Controllers;

/// <summary>
/// Read-only portal for parents/guardians, scoped to their own children. Access control is
/// by ownership (the caller's guardian record), so no per-endpoint permission is required
/// beyond authentication.
/// </summary>
[Authorize]
[Route("api/portal")]
public class ParentPortalController : BaseApiController
{
    [HttpGet("children")]
    public async Task<IActionResult> GetMyChildren()
    {
        var result = await Mediator.Send(new GetMyChildrenQuery());
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpGet("children/{studentId:guid}/finance")]
    public async Task<IActionResult> GetChildFinance(Guid studentId)
    {
        var result = await Mediator.Send(new GetMyChildFinanceQuery(studentId));
        return result.IsSuccess ? Ok(result.Data) : NotFound(result.Error);
    }

    [HttpGet("children/{studentId:guid}/results")]
    public async Task<IActionResult> GetChildResults(Guid studentId, [FromQuery] Guid? termId)
    {
        var result = await Mediator.Send(new GetMyChildResultsQuery { StudentId = studentId, AcademicTermId = termId });
        return result.IsSuccess ? Ok(result.Data) : NotFound(result.Error);
    }

    [HttpGet("children/{studentId:guid}/attendance")]
    public async Task<IActionResult> GetChildAttendance(Guid studentId, [FromQuery] Guid? termId)
    {
        var result = await Mediator.Send(new GetMyChildAttendanceQuery { StudentId = studentId, AcademicTermId = termId });
        return result.IsSuccess ? Ok(result.Data) : NotFound(result.Error);
    }
}
