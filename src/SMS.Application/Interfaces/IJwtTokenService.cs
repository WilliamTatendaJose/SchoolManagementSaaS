namespace SMS.Application.Interfaces;

/// <summary>
/// Interface for JWT token generation
/// </summary>
public interface IJwtTokenService
{
    string GenerateAccessToken(Guid userId, string email, Guid tenantId, IEnumerable<string> roles);
    string GenerateRefreshToken();
    (Guid UserId, Guid TenantId)? ValidateToken(string token);
}
