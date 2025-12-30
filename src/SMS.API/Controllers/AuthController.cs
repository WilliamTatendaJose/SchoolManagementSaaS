using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;
using SMS.Domain.Enums;

namespace SMS.API.Controllers;

/// <summary>
/// API endpoints for authentication
/// </summary>
[EnableRateLimiting("AuthLimit")]
public class AuthController : BaseApiController
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IMemoryCache _cache;

    public AuthController(
        IApplicationDbContext context, 
        IJwtTokenService jwtTokenService,
        IMemoryCache cache)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _cache = cache;
    }

    /// <summary>
    /// Get available tenants for login (public endpoint)
    /// </summary>
    [HttpGet("tenants")]
    public async Task<IActionResult> GetTenants()
    {
        const string cacheKey = "active_tenants";
        
        if (!_cache.TryGetValue(cacheKey, out List<TenantInfo>? tenants))
        {
            tenants = await _context.Tenants
                .AsNoTracking()
                .Where(t => t.Status == TenantStatus.Active)
                .Select(t => new TenantInfo
                {
                    Id = t.Id,
                    Name = t.Name,
                    Code = t.Code
                })
                .ToListAsync();

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
            
            _cache.Set(cacheKey, tenants, cacheOptions);
        }

        return Ok(tenants);
    }

    /// <summary>
    /// Authenticate user and return JWT token
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // Use IgnoreQueryFilters to bypass tenant filter during login
        var user = await _context.Users
            .IgnoreQueryFilters()
            .Include(u => u.Roles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower() 
                && u.TenantId == request.TenantId 
                && !u.IsDeleted);

        if (user == null)
        {
            return Unauthorized(new { Message = "Invalid email or password" });
        }

        if (!VerifyPassword(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { Message = "Invalid email or password" });
        }

        if (!user.IsActive)
        {
            return Unauthorized(new { Message = "Account is deactivated" });
        }

        var roles = user.Roles.Select(ur => ur.Role.Name).ToList();
        var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email, user.TenantId, roles);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        user.LastLoginAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = 3600,
            User = new UserInfo
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = roles
            }
        });
    }

    /// <summary>
    /// Logout user and invalidate refresh token
    /// </summary>
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            return BadRequest(new { Message = "Invalid user" });
        }

        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userGuid && !u.IsDeleted);

        if (user != null)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            await _context.SaveChangesAsync();
        }

        return Ok(new { Message = "Logged out successfully" });
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var tokenData = _jwtTokenService.ValidateToken(request.AccessToken);
        
        if (tokenData == null)
        {
            return Unauthorized(new { Message = "Invalid token" });
        }

        // Use IgnoreQueryFilters to bypass tenant filter during refresh
        var user = await _context.Users
            .IgnoreQueryFilters()
            .Include(u => u.Roles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == tokenData.Value.UserId && !u.IsDeleted);

        if (user == null || user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime < DateTime.UtcNow)
        {
            return Unauthorized(new { Message = "Invalid or expired refresh token" });
        }

        var roles = user.Roles.Select(ur => ur.Role.Name).ToList();
        var newAccessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email, user.TenantId, roles);
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        await _context.SaveChangesAsync();

        return Ok(new LoginResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresIn = 3600
        });
    }

    private static bool VerifyPassword(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}

public record LoginRequest
{
    public Guid TenantId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public record RefreshTokenRequest
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
}

public record LoginResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public int ExpiresIn { get; init; }
    public UserInfo? User { get; init; }
}

public record UserInfo
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public List<string> Roles { get; init; } = [];
}

public record TenantInfo
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
}
