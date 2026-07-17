using Microsoft.EntityFrameworkCore;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Notifications;

internal record ResolvedRecipient
{
    public required Guid StudentId { get; init; }
    public Guid? GuardianId { get; init; }
    public required string Name { get; init; }
    public required string Phone { get; init; }
}

/// <summary>
/// Resolves the guardian who should receive a notification for each student: the primary
/// contact with a phone number, falling back to any guardian with one. Students with no
/// reachable guardian are simply omitted.
/// </summary>
internal static class RecipientResolver
{
    public static async Task<List<ResolvedRecipient>> ResolveForStudentsAsync(
        IApplicationDbContext context, IReadOnlyCollection<Guid> studentIds, CancellationToken cancellationToken)
    {
        if (studentIds.Count == 0)
        {
            return [];
        }

        var links = await context.StudentGuardians
            .Where(sg => studentIds.Contains(sg.StudentId)
                && sg.Guardian.Phone != null
                && sg.Guardian.Phone != "")
            .Select(sg => new
            {
                sg.StudentId,
                sg.GuardianId,
                sg.IsPrimaryContact,
                GuardianName = sg.Guardian.FirstName + " " + sg.Guardian.LastName,
                Phone = sg.Guardian.Phone!
            })
            .ToListAsync(cancellationToken);

        return links
            .GroupBy(l => l.StudentId)
            .Select(group =>
            {
                var chosen = group.OrderByDescending(x => x.IsPrimaryContact).First();
                return new ResolvedRecipient
                {
                    StudentId = chosen.StudentId,
                    GuardianId = chosen.GuardianId,
                    Name = chosen.GuardianName,
                    Phone = chosen.Phone
                };
            })
            .ToList();
    }
}
