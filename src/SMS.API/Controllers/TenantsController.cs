using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;
using SMS.Domain.Enums;

namespace SMS.API.Controllers;

/// <summary>
/// API endpoints for tenant (school) management - Super Admin only
/// </summary>
[Authorize(Roles = "SuperAdmin")]
public class TenantsController : BaseApiController
{
    private readonly IApplicationDbContext _context;

    public TenantsController(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get all tenants
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTenants([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var query = _context.Tenants.AsNoTracking();
        
        var total = await query.CountAsync();
        var tenants = await query
            .OrderBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TenantDto
            {
                Id = t.Id,
                Name = t.Name,
                Code = t.Code,
                Email = t.Email,
                Phone = t.Phone,
                Status = t.Status.ToString(),
                SubscriptionPlan = t.SubscriptionPlan.ToString(),
                MaxStudents = t.MaxStudents,
                SubscriptionEndDate = t.SubscriptionEndDate,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        return Ok(new { Data = tenants, Total = total, Page = page, PageSize = pageSize });
    }

    /// <summary>
    /// Get tenant by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTenant(Guid id)
    {
        var tenant = await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tenant == null)
            return NotFound();

        return Ok(tenant);
    }

    /// <summary>
    /// Create a new tenant (school)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantRequest request)
    {
        var tenant = new Tenant
        {
            Name = request.Name,
            Code = request.Code,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address,
            City = request.City,
            Country = request.Country ?? "Zimbabwe",
            Status = TenantStatus.Active,
            SubscriptionPlan = Enum.Parse<SubscriptionPlan>(request.SubscriptionPlan ?? "Basic"),
            MaxStudents = request.MaxStudents ?? 100,
            SubscriptionStartDate = DateTime.UtcNow,
            SubscriptionEndDate = DateTime.UtcNow.AddMonths(12)
        };

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTenant), new { id = tenant.Id }, tenant);
    }

    /// <summary>
    /// Update tenant status
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateTenantStatus(Guid id, [FromBody] UpdateTenantStatusRequest request)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == id);

        if (tenant == null)
            return NotFound();

        tenant.Status = Enum.Parse<TenantStatus>(request.Status);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public record TenantDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string Status { get; init; } = string.Empty;
    public string SubscriptionPlan { get; init; } = string.Empty;
    public int MaxStudents { get; init; }
    public DateTime? SubscriptionEndDate { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateTenantRequest
{
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? Country { get; init; }
    public string? SubscriptionPlan { get; init; }
    public int? MaxStudents { get; init; }
}

public record UpdateTenantStatusRequest
{
    public string Status { get; init; } = string.Empty;
}
