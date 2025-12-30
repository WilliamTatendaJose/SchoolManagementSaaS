using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Users.Queries;

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Result<UserDetailDto>>
{
    private readonly IApplicationDbContext _context;

    public GetUserByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<UserDetailDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.Roles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.Permissions)
                        .ThenInclude(rp => rp.Permission)
            .Include(u => u.Staff)
            .Include(u => u.Guardian)
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

        if (user == null)
        {
            return Result<UserDetailDto>.Failure("User not found");
        }

        // Get all permissions from user's roles
        var permissions = user.Roles
            .SelectMany(ur => ur.Role.Permissions)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToList();

        var dto = new UserDetailDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            Phone = user.Phone,
            ProfilePicture = user.ProfilePicture,
            IsActive = user.IsActive,
            EmailConfirmed = user.EmailConfirmed,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt,
            Roles = user.Roles.Select(ur => new RoleDto
            {
                Id = ur.Role.Id,
                Name = ur.Role.Name,
                Description = ur.Role.Description
            }).ToList(),
            Permissions = permissions,
            StaffId = user.Staff?.Id,
            StaffNumber = user.Staff?.StaffNumber,
            GuardianId = user.Guardian?.Id
        };

        return Result<UserDetailDto>.Success(dto);
    }
}
