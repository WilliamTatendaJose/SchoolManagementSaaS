using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Security;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Infrastructure.Authorization;

namespace SMS.API.Controllers;

/// <summary>
/// API endpoints for attendance management
/// </summary>
[Authorize]
public class AttendanceController : BaseApiController
{
    private readonly IApplicationDbContext _context;
    private readonly ISmsService _smsService;

    public AttendanceController(IApplicationDbContext context, ISmsService smsService)
    {
        _context = context;
        _smsService = smsService;
    }

    /// <summary>
    /// Get attendance records for a class on a specific date
    /// </summary>
    [HttpGet("class/{classId:guid}")]
    [RequirePermission(Permissions.AttendanceView)]
    public async Task<IActionResult> GetClassAttendance(Guid classId, [FromQuery] DateTime date)
    {
        var attendance = await _context.Attendances
            .AsNoTracking()
            .Where(a => a.ClassId == classId && a.Date.Date == date.Date)
            .Include(a => a.Student)
            .Select(a => new AttendanceDto
            {
                Id = a.Id,
                StudentId = a.StudentId,
                StudentName = a.Student.FullName,
                StudentNumber = a.Student.StudentNumber,
                Status = a.Status.ToString(),
                TimeIn = a.TimeIn,
                Reason = a.Reason
            })
            .ToListAsync();

        return Ok(attendance);
    }

    /// <summary>
    /// Mark attendance for multiple students
    /// </summary>
    [HttpPost]
    [RequirePermission(Permissions.AttendanceMark)]
    public async Task<IActionResult> MarkAttendance([FromBody] MarkAttendanceRequest request)
    {
        // MarkedById is a Staff FK, not the ASP.NET Identity user id - the claim gives us
        // the latter, so it must be resolved to the matching staff record (same pattern as
        // DisciplineController's ReportedById lookup) rather than parsed directly.
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        Guid? staffId = null;
        if (Guid.TryParse(currentUserId, out var userId))
        {
            staffId = await _context.Staff
                .Where(s => s.UserId == userId)
                .Select(s => (Guid?)s.Id)
                .FirstOrDefaultAsync();
        }

        var studentIds = request.Records.Select(r => r.StudentId).ToList();
        var existingRecords = await _context.Attendances
            .Where(a => a.ClassId == request.ClassId && a.Date.Date == request.Date.Date && studentIds.Contains(a.StudentId))
            .ToDictionaryAsync(a => a.StudentId);

        var savedCount = 0;
        var absences = new List<(Guid StudentId, string StudentName)>();

        foreach (var record in request.Records)
        {
            var status = Enum.Parse<AttendanceStatus>(record.Status);

            // Re-marking the same student/class/date updates the existing row instead of
            // inserting a duplicate, which would otherwise double-count in summaries and
            // show repeated rows for one day.
            if (existingRecords.TryGetValue(record.StudentId, out var existing))
            {
                existing.SubjectId = request.SubjectId;
                existing.Status = status;
                existing.TimeIn = record.TimeIn;
                existing.Reason = record.Reason;
                existing.MarkedById = staffId;
            }
            else
            {
                _context.Attendances.Add(new Attendance
                {
                    StudentId = record.StudentId,
                    ClassId = request.ClassId,
                    SubjectId = request.SubjectId,
                    Date = request.Date,
                    Status = status,
                    TimeIn = record.TimeIn,
                    Reason = record.Reason,
                    MarkedById = staffId
                });
            }

            savedCount++;

            if (status == AttendanceStatus.Absent)
            {
                var student = await _context.Students.FindAsync(record.StudentId);
                if (student != null)
                {
                    absences.Add((student.Id, student.FullName));
                }
            }
        }

        await _context.SaveChangesAsync();

        // Send SMS notifications for absences
        if (request.SendNotifications && absences.Any())
        {
            await SendAbsenceNotificationsAsync(absences, request.Date);
        }

        return Ok(new { Message = $"{savedCount} attendance records saved" });
    }

    /// <summary>
    /// Get attendance summary for a student
    /// </summary>
    [HttpGet("student/{studentId:guid}/summary")]
    [RequirePermission(Permissions.AttendanceView)]
    public async Task<IActionResult> GetStudentAttendanceSummary(Guid studentId, [FromQuery] Guid? termId)
    {
        var query = _context.Attendances
            .AsNoTracking()
            .Where(a => a.StudentId == studentId);

        if (termId.HasValue)
        {
            var term = await _context.AcademicTerms.FindAsync(termId.Value);
            if (term != null)
            {
                query = query.Where(a => a.Date >= term.StartDate && a.Date <= term.EndDate);
            }
        }

        var records = await query.ToListAsync();

        var summary = new AttendanceSummaryDto
        {
            TotalDays = records.Count,
            PresentDays = records.Count(r => r.Status == AttendanceStatus.Present),
            AbsentDays = records.Count(r => r.Status == AttendanceStatus.Absent),
            LateDays = records.Count(r => r.Status == AttendanceStatus.Late),
            ExcusedDays = records.Count(r => r.Status == AttendanceStatus.Excused)
        };

        summary.AttendancePercentage = summary.TotalDays > 0
            ? Math.Round((decimal)(summary.PresentDays + summary.LateDays) / summary.TotalDays * 100, 2)
            : 0;

        return Ok(summary);
    }

    private async Task SendAbsenceNotificationsAsync(List<(Guid StudentId, string StudentName)> absences, DateTime date)
    {
        foreach (var (studentId, studentName) in absences)
        {
            var guardian = await _context.StudentGuardians
                .Where(sg => sg.StudentId == studentId && sg.IsPrimaryContact)
                .Include(sg => sg.Guardian)
                .Select(sg => sg.Guardian)
                .FirstOrDefaultAsync();

            if (guardian?.Phone != null)
            {
                var message = $"Dear Parent/Guardian, {studentName} was marked absent on {date:dd/MM/yyyy}. Please contact the school if this is unexpected.";
                await _smsService.SendSmsAsync(guardian.Phone, message);
            }
        }
    }
}

public record AttendanceDto
{
    public Guid Id { get; init; }
    public Guid StudentId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public string StudentNumber { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public TimeOnly? TimeIn { get; init; }
    public string? Reason { get; init; }
}

public record MarkAttendanceRequest
{
    public Guid ClassId { get; init; }
    public Guid? SubjectId { get; init; }
    public DateTime Date { get; init; }
    public bool SendNotifications { get; init; }
    public List<AttendanceRecord> Records { get; init; } = [];
}

public record AttendanceRecord
{
    public Guid StudentId { get; init; }
    public string Status { get; init; } = string.Empty;
    public TimeOnly? TimeIn { get; init; }
    public string? Reason { get; init; }
}

public record AttendanceSummaryDto
{
    public int TotalDays { get; set; }
    public int PresentDays { get; set; }
    public int AbsentDays { get; set; }
    public int LateDays { get; set; }
    public int ExcusedDays { get; set; }
    public decimal AttendancePercentage { get; set; }
}
