using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Security;
using SMS.Application.Features.Users.Commands;
using SMS.Application.Features.Users.Queries;
using SMS.Application.Interfaces;
using SMS.Infrastructure.Authorization;

namespace SMS.API.Controllers;

[Authorize]
public class UsersController : BaseApiController
{
    private readonly IApplicationDbContext _context;

    public UsersController(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Debug endpoint - Get current user's permissions (remove in production)
    /// </summary>
    [HttpGet("me/permissions")]
    public async Task<IActionResult> GetMyPermissions()
    {
        var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            return Unauthorized(new { Message = "Invalid user", Claims = User.Claims.Select(c => new { c.Type, c.Value }) });
        }

        // Get user roles first (ignoring query filters)
        var userRoles = await _context.UserRoles
            .IgnoreQueryFilters()
            .Where(ur => ur.UserId == userGuid && !ur.IsDeleted)
            .Select(ur => new { ur.RoleId, RoleName = ur.Role.Name })
            .ToListAsync();

        // Get permissions for those roles
        var roleIds = userRoles.Select(r => r.RoleId).ToList();
        var permissions = await _context.RolePermissions
            .IgnoreQueryFilters()
            .Where(rp => roleIds.Contains(rp.RoleId) && !rp.IsDeleted)
            .Select(rp => new { rp.Permission.Name, rp.Permission.Code })
            .Distinct()
            .ToListAsync();

        return Ok(new
        {
            UserId = userGuid,
            Roles = userRoles,
            RoleCount = userRoles.Count,
            PermissionCount = permissions.Count,
            Permissions = permissions
        });
    }

    /// <summary>
    /// Get paginated list of users
    /// </summary>
    [HttpGet]
    [RequirePermission(Permissions.UsersView)]
    public async Task<IActionResult> GetUsers([FromQuery] GetUsersQuery query)
    {
        var result = await Mediator.Send(query);
        
        if (!result.IsSuccess)
            return BadRequest(result.Error);
            
        return Ok(result.Data);
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.UsersView)]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var result = await Mediator.Send(new GetUserByIdQuery(id));
        
        if (!result.IsSuccess)
            return NotFound(result.Error);
            
        return Ok(result.Data);
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    [HttpPost]
    [RequirePermission(Permissions.UsersCreate)]
    public async Task<IActionResult> RegisterUser([FromBody] RegisterUserCommand command)
    {
        var result = await Mediator.Send(command);
        
        if (!result.IsSuccess)
            return BadRequest(result.Error);
            
        return CreatedAtAction(nameof(GetUser), new { id = result.Data }, new { Id = result.Data });
    }

    /// <summary>
    /// Update user details
    /// </summary>
    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.UsersEdit)]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch");

        var result = await Mediator.Send(command);
        
        if (!result.IsSuccess)
            return BadRequest(result.Error);
            
        return NoContent();
    }

    /// <summary>
    /// Assign roles to a user
    /// </summary>
    [HttpPut("{id:guid}/roles")]
    [RequirePermission(Permissions.RolesManage)]
    public async Task<IActionResult> AssignRoles(Guid id, [FromBody] List<string> roles)
    {
        var result = await Mediator.Send(new AssignRolesCommand
        {
            UserId = id,
            Roles = roles
        });
        
        if (!result.IsSuccess)
            return BadRequest(result.Error);
            
        return NoContent();
    }

    /// <summary>
    /// Change user password (by user themselves)
    /// </summary>
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized();

        var result = await Mediator.Send(new ChangePasswordCommand
        {
            UserId = userGuid,
            CurrentPassword = request.CurrentPassword,
            NewPassword = request.NewPassword
        });
        
        if (!result.IsSuccess)
            return BadRequest(result.Error);
            
        return Ok(new { Message = "Password changed successfully" });
    }

    /// <summary>
    /// Reset user password (admin function)
    /// </summary>
    [HttpPost("{id:guid}/reset-password")]
    [RequirePermission(Permissions.UsersEdit)]
    public async Task<IActionResult> ResetPassword(Guid id)
    {
        var result = await Mediator.Send(new ResetPasswordCommand { UserId = id });
        
        if (!result.IsSuccess)
            return BadRequest(result.Error);
            
        return Ok(new { TemporaryPassword = result.Data });
    }

    /// <summary>
    /// Get current user profile
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized();

        var result = await Mediator.Send(new GetUserByIdQuery(userGuid));

        if (!result.IsSuccess)
            return NotFound(result.Error);

        return Ok(result.Data);
    }

    /// <summary>
    /// Update the current user's own profile (name/phone). Deliberately not gated by
    /// users.edit - every authenticated user can maintain their own profile, but only
    /// their own: the target id always comes from the JWT, never from the request body.
    /// </summary>
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateMyProfileRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized();

        var result = await Mediator.Send(new UpdateMyProfileCommand
        {
            UserId = userGuid,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone
        });

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return NoContent();
    }
}

public record UpdateMyProfileRequest
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Phone { get; init; }
}

public record ChangePasswordRequest
{
    public string CurrentPassword { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
}
