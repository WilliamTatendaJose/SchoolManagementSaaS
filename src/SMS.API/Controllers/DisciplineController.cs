using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common.Security;
using SMS.Application.Features.Discipline.Commands;
using SMS.Application.Features.Discipline.Queries;
using SMS.Infrastructure.Authorization;

namespace SMS.API.Controllers;

/// <summary>
/// Student discipline records, with an optional guardian notification on creation.
/// </summary>
[Authorize]
public class DisciplineController : BaseApiController
{
    [HttpGet]
    [RequirePermission(Permissions.DisciplineView)]
    public async Task<IActionResult> GetRecords([FromQuery] GetDisciplineRecordsQuery query)
    {
        var result = await Mediator.Send(query);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpPost]
    [RequirePermission(Permissions.DisciplineManage)]
    public async Task<IActionResult> CreateRecord([FromBody] CreateDisciplineRecordCommand command)
    {
        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(new { Id = result.Data }) : BadRequest(result.Error);
    }
}
