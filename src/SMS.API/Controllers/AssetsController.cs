using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common.Security;
using SMS.Application.Features.Assets.Commands;
using SMS.Application.Features.Assets.Queries;
using SMS.Infrastructure.Authorization;

namespace SMS.API.Controllers;

/// <summary>
/// School asset register.
/// </summary>
[Authorize]
public class AssetsController : BaseApiController
{
    [HttpGet]
    [RequirePermission(Permissions.AssetsView)]
    public async Task<IActionResult> GetAssets([FromQuery] GetAssetsQuery query)
    {
        var result = await Mediator.Send(query);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpPost]
    [RequirePermission(Permissions.AssetsManage)]
    public async Task<IActionResult> CreateAsset([FromBody] CreateAssetCommand command)
    {
        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(new { Id = result.Data }) : BadRequest(result.Error);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.AssetsManage)]
    public async Task<IActionResult> UpdateAsset(Guid id, [FromBody] UpdateAssetCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch");

        var result = await Mediator.Send(command);
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }
}
