using Microsoft.AspNetCore.Authorization;

namespace SMS.Infrastructure.Authorization;

/// <summary>
/// Attribute for requiring a specific permission on a controller or action
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequirePermissionAttribute : AuthorizeAttribute
{
    private const string PolicyPrefix = "Permission:";

    public RequirePermissionAttribute(string permission)
        : base($"{PolicyPrefix}{permission}")
    {
    }
}
