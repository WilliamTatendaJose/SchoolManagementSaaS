using Microsoft.EntityFrameworkCore;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Timetable;

/// <summary>
/// Detects scheduling conflicts for a timetable slot: a class, teacher, or classroom cannot
/// be double-booked at overlapping times on the same day within a term. Overlap is evaluated
/// in memory to stay independent of provider-specific TimeOnly SQL translation.
/// </summary>
internal static class TimetableClashDetector
{
    public static async Task<string?> FindClashAsync(
        IApplicationDbContext context,
        Guid academicTermId,
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        Guid classId,
        Guid teacherId,
        Guid? classroomId,
        Guid? excludeSlotId,
        CancellationToken cancellationToken)
    {
        var sameDaySlots = await context.TimetableSlots
            .Where(s => s.AcademicTermId == academicTermId
                && s.DayOfWeek == dayOfWeek
                && (excludeSlotId == null || s.Id != excludeSlotId))
            .Select(s => new { s.ClassId, s.TeacherId, s.ClassroomId, s.StartTime, s.EndTime })
            .ToListAsync(cancellationToken);

        bool Overlaps(TimeOnly otherStart, TimeOnly otherEnd) => startTime < otherEnd && otherStart < endTime;

        if (sameDaySlots.Any(s => s.ClassId == classId && Overlaps(s.StartTime, s.EndTime)))
        {
            return "The class already has a lesson scheduled at this time";
        }

        if (sameDaySlots.Any(s => s.TeacherId == teacherId && Overlaps(s.StartTime, s.EndTime)))
        {
            return "The teacher is already scheduled at this time";
        }

        if (classroomId.HasValue
            && sameDaySlots.Any(s => s.ClassroomId == classroomId && Overlaps(s.StartTime, s.EndTime)))
        {
            return "The classroom is already booked at this time";
        }

        return null;
    }
}
