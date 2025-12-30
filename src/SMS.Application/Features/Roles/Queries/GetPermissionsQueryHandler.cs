using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Roles.Queries;

public class GetPermissionsQueryHandler : IRequestHandler<GetPermissionsQuery, Result<List<PermissionGroupDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetPermissionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<PermissionGroupDto>>> Handle(GetPermissionsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Permissions.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Module))
        {
            query = query.Where(p => p.Module == request.Module);
        }

        var permissions = await query
            .OrderBy(p => p.Module)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);

        var groups = permissions
            .GroupBy(p => p.Module)
            .Select(g => new PermissionGroupDto
            {
                Module = g.Key,
                Permissions = g.Select(p => new PermissionDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Code = p.Code,
                    Description = p.Description,
                    Module = p.Module
                }).ToList()
            })
            .OrderBy(g => g.Module)
            .ToList();

        return Result<List<PermissionGroupDto>>.Success(groups);
    }
}
