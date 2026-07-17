namespace SMS.Application.Interfaces;

/// <summary>
/// Interface for accessing current user information
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    Guid? TenantId { get; }
    IEnumerable<string> Roles { get; }
    bool IsAuthenticated { get; }

    /// <summary>Source IP of the current request, when available.</summary>
    string? IpAddress { get; }

    /// <summary>User-Agent of the current request, when available.</summary>
    string? UserAgent { get; }
}
