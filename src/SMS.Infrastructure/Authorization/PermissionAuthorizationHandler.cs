using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SMS.Infrastructure.Persistence;
using System.Security.Claims;

namespace SMS.Infrastructure.Authorization;

/// <summary>
/// Handles permission-based authorization
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ApplicationDbContext _dbContext;

    // Injected directly (this handler is registered Scoped) rather than resolved via a
    // manually-created IServiceScope: creating a new scope hands back a fresh
    // ITenantService/DbContext pair, discarding the tenant TenantMiddleware already set
    // on the current request's scope, so every permission check would always fail.
    public PermissionAuthorizationHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            return;
        }

        // Check if user has the required permission through their roles
        var hasPermission = await _dbContext.UserRoles
            .Where(ur => ur.UserId == userGuid)
            .SelectMany(ur => ur.Role.Permissions)
            .AnyAsync(rp => rp.Permission.Code == requirement.Permission);

        if (hasPermission)
        {
            context.Succeed(requirement);
        }
    }
}
