using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SMS.Infrastructure.Persistence;
using System.Security.Claims;

namespace SMS.Infrastructure.Authorization;

/// <summary>
/// Handles permission-based authorization
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IServiceProvider _serviceProvider;

    public PermissionAuthorizationHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
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
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var hasPermission = await dbContext.UserRoles
            .Where(ur => ur.UserId == userGuid)
            .SelectMany(ur => ur.Role.Permissions)
            .AnyAsync(rp => rp.Permission.Code == requirement.Permission);

        if (hasPermission)
        {
            context.Succeed(requirement);
        }
    }
}
