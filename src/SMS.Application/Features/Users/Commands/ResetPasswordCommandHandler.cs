using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using System.Security.Cryptography;

namespace SMS.Application.Features.Users.Commands;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result<string>>
{
    private readonly IApplicationDbContext _context;

    public ResetPasswordCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<string>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            return Result<string>.Failure("User not found");
        }

        // Generate temporary password
        var tempPassword = GenerateTemporaryPassword();

        // Hash and set new password
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);

        await _context.SaveChangesAsync(cancellationToken);

        return Result<string>.Success(tempPassword);
    }

    private static string GenerateTemporaryPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#$";
        var random = RandomNumberGenerator.Create();
        var bytes = new byte[12];
        random.GetBytes(bytes);
        
        return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
    }
}
