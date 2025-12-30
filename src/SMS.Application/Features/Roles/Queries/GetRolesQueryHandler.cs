using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Roles.Queries;

public class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, Result<List<RoleDetailDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetRolesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<RoleDetailDto>>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await _context.Roles
            .AsNoTracking()
            .Include(r => r.Permissions)
                .ThenInclude(rp => rp.Permission)
            .Include(r => r.UserRoles)
            .OrderBy(r => r.Name)
            .Select(r => new RoleDetailDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                IsSystemRole = r.IsSystemRole,
                UserCount = r.UserRoles.Count,
                Permissions = r.Permissions.Select(rp => new PermissionDto
                {
                    Id = rp.Permission.Id,
                    Name = rp.Permission.Name,
                    Code = rp.Permission.Code,
                    Description = rp.Permission.Description,
                    Module = rp.Permission.Module
                }).OrderBy(p => p.Module).ThenBy(p => p.Name).ToList()
            })
            .ToListAsync(cancellationToken);

        return Result<List<RoleDetailDto>>.Success(roles);
    }
}
