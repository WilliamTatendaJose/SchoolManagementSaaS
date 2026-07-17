using Microsoft.EntityFrameworkCore;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.ParentPortal;

/// <summary>
/// Resolves which students belong to the signed-in parent, via their guardian record
/// (User -> Guardian.UserId -> StudentGuardian -> Student). This is the security boundary
/// for the parent portal: a parent can only ever reach their own children's data.
/// </summary>
internal static class ParentChildAccess
{
    public static async Task<List<Guid>> GetChildStudentIdsAsync(
        IApplicationDbContext context, Guid? userId, CancellationToken cancellationToken)
    {
        if (userId is null)
        {
            return [];
        }

        var guardianId = await context.Guardians
            .Where(g => g.UserId == userId)
            .Select(g => (Guid?)g.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (guardianId is null)
        {
            return [];
        }

        return await context.StudentGuardians
            .Where(sg => sg.GuardianId == guardianId)
            .Select(sg => sg.StudentId)
            .ToListAsync(cancellationToken);
    }

    public static async Task<bool> OwnsStudentAsync(
        IApplicationDbContext context, Guid? userId, Guid studentId, CancellationToken cancellationToken)
    {
        var childIds = await GetChildStudentIdsAsync(context, userId, cancellationToken);
        return childIds.Contains(studentId);
    }
}
