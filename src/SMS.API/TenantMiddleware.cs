using SMS.Application.Interfaces;
using System.Security.Claims;

namespace SMS.API;

/// <summary>
/// Middleware to extract and set tenant context from JWT claims
/// </summary>
public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var tenantIdClaim = context.User.FindFirst("tenant_id")?.Value;
            
            if (Guid.TryParse(tenantIdClaim, out var tenantId))
            {
                tenantService.SetCurrentTenant(tenantId);
            }
        }
        else
        {
            // Check for tenant header for public endpoints
            var tenantHeader = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
            
            if (Guid.TryParse(tenantHeader, out var tenantId))
            {
                tenantService.SetCurrentTenant(tenantId);
            }
        }

        await _next(context);
    }
}
