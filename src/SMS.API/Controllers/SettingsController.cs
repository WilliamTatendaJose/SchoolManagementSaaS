using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Security;
using SMS.Application.Interfaces;
using SMS.Infrastructure.Authorization;

namespace SMS.API.Controllers;

/// <summary>
/// School profile settings for the current tenant. Subscription plan, seat limits, SMS
/// credits and feature-module flags are read-only here - they're managed exclusively by
/// SuperAdmin via TenantsController, never self-service, so a school can't unlock a paid
/// module (or a bigger seat count) by just editing its own settings.
/// </summary>
[Authorize]
public class SettingsController : BaseApiController
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public SettingsController(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    [HttpGet]
    [RequirePermission(Permissions.SettingsView)]
    public async Task<IActionResult> GetSettings()
    {
        var tenant = await _context.Tenants
            .AsNoTracking()
            .Where(t => t.Id == _currentUser.TenantId)
            .Select(t => new SchoolSettingsDto
            {
                Id = t.Id,
                Name = t.Name,
                Code = t.Code,
                Logo = t.Logo,
                PrimaryColor = t.PrimaryColor,
                AccentColor = t.AccentColor,
                Address = t.Address,
                City = t.City,
                Country = t.Country,
                Phone = t.Phone,
                Email = t.Email,
                Website = t.Website,
                TimeZone = t.TimeZone,
                Currency = t.Currency,
                Status = t.Status.ToString(),
                SubscriptionPlan = t.SubscriptionPlan.ToString(),
                SubscriptionStartDate = t.SubscriptionStartDate,
                SubscriptionEndDate = t.SubscriptionEndDate,
                MaxStudents = t.MaxStudents,
                CurrentStudentCount = t.Students.Count(),
                SmsCredits = t.SmsCredits,
                HasLmsModule = t.HasLmsModule,
                HasTransportModule = t.HasTransportModule,
                HasHostelModule = t.HasHostelModule,
                HasLibraryModule = t.HasLibraryModule
            })
            .FirstOrDefaultAsync();

        if (tenant == null)
            return NotFound("Tenant not found");

        return Ok(tenant);
    }

    [HttpPut]
    [RequirePermission(Permissions.SettingsManage)]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateSchoolSettingsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("School name is required");

        // The logo is stored inline as a data URI (embeds in PDFs + renders in the UI with
        // no image host). Cap it so a large upload can't bloat the tenant row / every PDF.
        // ~1MB of base64 ≈ a 750KB image, ample for a logo.
        if (request.Logo is { Length: > 1_400_000 })
            return BadRequest("Logo is too large. Please use an image under 750KB.");

        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == _currentUser.TenantId);
        if (tenant == null)
            return NotFound("Tenant not found");

        tenant.Name = request.Name.Trim();
        tenant.Logo = request.Logo;
        tenant.PrimaryColor = request.PrimaryColor;
        tenant.AccentColor = request.AccentColor;
        tenant.Address = request.Address;
        tenant.City = request.City;
        tenant.Country = request.Country;
        tenant.Phone = request.Phone;
        tenant.Email = request.Email;
        tenant.Website = request.Website;
        tenant.TimeZone = request.TimeZone;
        tenant.Currency = request.Currency;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// The current tenant's subscription plan and which feature modules are switched on.
    /// Deliberately not permission-gated beyond authentication: every user in the tenant
    /// (whatever their role) needs this to know which nav items/routes to show, not just
    /// whoever holds settings.view.
    /// </summary>
    [HttpGet("features")]
    public async Task<IActionResult> GetFeatures()
    {
        var features = await _context.Tenants
            .AsNoTracking()
            .Where(t => t.Id == _currentUser.TenantId)
            .Select(t => new TenantFeaturesDto
            {
                SubscriptionPlan = t.SubscriptionPlan.ToString(),
                HasLmsModule = t.HasLmsModule,
                HasTransportModule = t.HasTransportModule,
                HasHostelModule = t.HasHostelModule,
                HasLibraryModule = t.HasLibraryModule
            })
            .FirstOrDefaultAsync();

        if (features == null)
            return NotFound("Tenant not found");

        return Ok(features);
    }
}

public record SchoolSettingsDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? Logo { get; init; }
    public string? PrimaryColor { get; init; }
    public string? AccentColor { get; init; }
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? Country { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Website { get; init; }
    public string? TimeZone { get; init; }
    public string? Currency { get; init; }
    public string Status { get; init; } = string.Empty;
    public string SubscriptionPlan { get; init; } = string.Empty;
    public DateTime? SubscriptionStartDate { get; init; }
    public DateTime? SubscriptionEndDate { get; init; }
    public int MaxStudents { get; init; }
    public int CurrentStudentCount { get; init; }
    public int SmsCredits { get; init; }
    public bool HasLmsModule { get; init; }
    public bool HasTransportModule { get; init; }
    public bool HasHostelModule { get; init; }
    public bool HasLibraryModule { get; init; }
}

public record UpdateSchoolSettingsRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Logo { get; init; }
    public string? PrimaryColor { get; init; }
    public string? AccentColor { get; init; }
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? Country { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Website { get; init; }
    public string? TimeZone { get; init; }
    public string? Currency { get; init; }
}

public record TenantFeaturesDto
{
    public string SubscriptionPlan { get; init; } = string.Empty;
    public bool HasLmsModule { get; init; }
    public bool HasTransportModule { get; init; }
    public bool HasHostelModule { get; init; }
    public bool HasLibraryModule { get; init; }
}
