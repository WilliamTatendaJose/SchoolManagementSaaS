using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;

namespace SMS.Application.Features.Users.Commands;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public RegisterUserCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        // Check if email already exists
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (existingUser != null)
        {
            return Result<Guid>.Failure("A user with this email already exists");
        }

        // Hash password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var user = new User
        {
            Email = request.Email,
            PasswordHash = passwordHash,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            IsActive = true,
            EmailConfirmed = false
        };

        _context.Users.Add(user);

        // Assign roles
        foreach (var roleName in request.Roles)
        {
            var role = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == roleName, cancellationToken);

            if (role == null)
            {
                return Result<Guid>.Failure($"Role '{roleName}' not found");
            }

            _context.UserRoles.Add(new UserRole
            {
                User = user,
                Role = role
            });
        }

        // Link to staff if provided
        if (request.StaffId.HasValue)
        {
            var staff = await _context.Staff.FindAsync([request.StaffId.Value], cancellationToken);
            if (staff != null)
            {
                staff.UserId = user.Id;
            }
        }

        // Link to guardian if provided
        if (request.GuardianId.HasValue)
        {
            var guardian = await _context.Guardians.FindAsync([request.GuardianId.Value], cancellationToken);
            if (guardian != null)
            {
                guardian.UserId = user.Id;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(user.Id);
    }
}
