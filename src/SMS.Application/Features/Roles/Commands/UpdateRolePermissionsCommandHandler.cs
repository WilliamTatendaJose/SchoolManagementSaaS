using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;

namespace SMS.Application.Features.Roles.Commands;

public class UpdateRolePermissionsCommandHandler : IRequestHandler<UpdateRolePermissionsCommand, Common.Models.Result>
{
    private readonly IApplicationDbContext _context;

    public UpdateRolePermissionsCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Common.Models.Result> Handle(UpdateRolePermissionsCommand request, CancellationToken cancellationToken)
    {
        var role = await _context.Roles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);

        if (role == null)
        {
            return Common.Models.Result.Failure("Role not found");
        }

        if (role.IsSystemRole)
        {
            return Common.Models.Result.Failure("Cannot modify permissions for system roles");
        }

        // Hard-delete existing permissions. A plain Remove() is turned into a soft-delete
        // by SaveChanges (RolePermission is a BaseEntity), but the permission checks
        // (PermissionAuthorizationHandler, GetUserByIdQuery) don't filter IsDeleted - so a
        // soft-deleted mapping would still grant access, making a permission removal here
        // silently ineffective. ExecuteDelete issues a real DELETE that actually revokes it.
        await _context.RolePermissions
            .Where(rp => rp.RoleId == request.RoleId)
            .ExecuteDeleteAsync(cancellationToken);

        // Add new permissions
        foreach (var permissionCode in request.PermissionCodes)
        {
            var permission = await _context.Permissions
                .FirstOrDefaultAsync(p => p.Code == permissionCode, cancellationToken);

            if (permission != null)
            {
                _context.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permission.Id
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Common.Models.Result.Success();
    }
}
