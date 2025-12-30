using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;

namespace SMS.Application.Features.Roles.Commands;

public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateRoleCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        // Check if role already exists
        var existingRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == request.Name, cancellationToken);

        if (existingRole != null)
        {
            return Result<Guid>.Failure($"Role '{request.Name}' already exists");
        }

        var role = new Role
        {
            Name = request.Name,
            Description = request.Description,
            IsSystemRole = false
        };

        _context.Roles.Add(role);

        // Assign permissions
        foreach (var permissionCode in request.PermissionCodes)
        {
            var permission = await _context.Permissions
                .FirstOrDefaultAsync(p => p.Code == permissionCode, cancellationToken);

            if (permission != null)
            {
                _context.RolePermissions.Add(new RolePermission
                {
                    Role = role,
                    Permission = permission
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(role.Id);
    }
}
