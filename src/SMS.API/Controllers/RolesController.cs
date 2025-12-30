using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common.Security;
using SMS.Application.Features.Roles.Commands;
using SMS.Application.Features.Roles.Queries;
using SMS.Infrastructure.Authorization;

namespace SMS.API.Controllers;

[Authorize]
public class RolesController : BaseApiController
{
    /// <summary>
    /// Get all roles with their permissions
    /// </summary>
    [HttpGet]
    [RequirePermission(Permissions.RolesManage)]
    public async Task<IActionResult> GetRoles()
    {
        var result = await Mediator.Send(new GetRolesQuery());
        
        if (!result.IsSuccess)
            return BadRequest(result.Error);
            
        return Ok(result.Data);
    }

    /// <summary>
    /// Create a new role
    /// </summary>
    [HttpPost]
    [RequirePermission(Permissions.RolesManage)]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleCommand command)
    {
        var result = await Mediator.Send(command);
        
        if (!result.IsSuccess)
            return BadRequest(result.Error);
            
        return Ok(new { Id = result.Data });
    }

    /// <summary>
    /// Update role permissions
    /// </summary>
    [HttpPut("{id:guid}/permissions")]
    [RequirePermission(Permissions.RolesManage)]
    public async Task<IActionResult> UpdateRolePermissions(Guid id, [FromBody] List<string> permissions)
    {
        var result = await Mediator.Send(new UpdateRolePermissionsCommand
        {
            RoleId = id,
            PermissionCodes = permissions
        });
        
        if (!result.IsSuccess)
            return BadRequest(result.Error);
            
        return NoContent();
    }

    /// <summary>
    /// Get all available permissions
    /// </summary>
    [HttpGet("permissions")]
    [RequirePermission(Permissions.RolesManage)]
    public async Task<IActionResult> GetPermissions([FromQuery] string? module)
    {
        var result = await Mediator.Send(new GetPermissionsQuery { Module = module });
        
        if (!result.IsSuccess)
            return BadRequest(result.Error);
            
        return Ok(result.Data);
    }
}
