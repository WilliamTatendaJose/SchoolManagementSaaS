using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;

namespace SMS.Application.Features.Users.Commands;

public class AssignRolesCommandHandler : IRequestHandler<AssignRolesCommand, Common.Models.Result>
{
    private readonly IApplicationDbContext _context;

    public AssignRolesCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Common.Models.Result> Handle(AssignRolesCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            return Common.Models.Result.Failure("User not found");
        }

        // Remove existing roles
        var existingUserRoles = await _context.UserRoles
            .Where(ur => ur.UserId == request.UserId)
            .ToListAsync(cancellationToken);

        foreach (var userRole in existingUserRoles)
        {
            _context.UserRoles.Remove(userRole);
        }

        // Add new roles
        foreach (var roleName in request.Roles)
        {
            var role = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == roleName, cancellationToken);

            if (role == null)
            {
                return Common.Models.Result.Failure($"Role '{roleName}' not found");
            }

            _context.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Common.Models.Result.Success();
    }
}
