using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Users.Commands;

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdateUserCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

        if (user == null)
        {
            return Result.Failure("User not found");
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Phone = request.Phone;
        user.IsActive = request.IsActive;

        // Reconcile the guardian <-> login link. Guardian.UserId is the FK the parent
        // portal resolves a parent's children through (ParentChildAccess), so this is the
        // link that actually makes a parent login "see" their child. Make the DB match the
        // requested guardian: unlink whichever guardian currently points at this login if
        // it isn't the requested one, then point the requested guardian here.
        var currentGuardian = await _context.Guardians
            .FirstOrDefaultAsync(g => g.UserId == user.Id, cancellationToken);

        if (currentGuardian?.Id != request.GuardianId)
        {
            if (currentGuardian != null)
            {
                currentGuardian.UserId = null;
            }

            if (request.GuardianId is { } guardianId)
            {
                var guardian = await _context.Guardians
                    .FirstOrDefaultAsync(g => g.Id == guardianId, cancellationToken);
                if (guardian == null)
                {
                    return Result.Failure("Guardian not found");
                }

                // A guardian maps to at most one login; assign it to this one.
                guardian.UserId = user.Id;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
