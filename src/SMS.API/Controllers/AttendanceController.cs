using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;
using SMS.Domain.Enums;

namespace SMS.API.Controllers;

/// <summary>
/// API endpoints for attendance management
/// </summary>
[Authorize]
public class AttendanceController : BaseApiController
{
    private readonly IApplicationDbContext _context;
    private readonly ISmsService _smsService;
    private readonly IWhatsAppService _whatsAppService;

    public AttendanceController(IApplicationDbContext context, ISmsService smsService, IWhatsAppService whatsAppService)
    {
        _context = context;
        _smsService = smsService;
        _whatsAppService = whatsAppService;
    }

    /// <summary>
    /// Get attendance records for a class on a specific date
    /// </summary>
    [HttpGet("class/{classId:guid}")]
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
    public async Task<IActionResult> MarkAttendance([FromBody] MarkAttendanceRequest request)
    {
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var staffId = Guid.TryParse(currentUserId, out var id) ? id : (Guid?)null;

        var attendanceRecords = new List<Attendance>();
        var absences = new List<(Guid StudentId, string StudentName)>();

        foreach (var record in request.Records)
        {
            var status = Enum.Parse<AttendanceStatus>(record.Status);
            
            var attendance = new Attendance
            {
                StudentId = record.StudentId,
                ClassId = request.ClassId,
                SubjectId = request.SubjectId,
                Date = request.Date,
                Status = status,
                TimeIn = record.TimeIn,
                Reason = record.Reason,
                MarkedById = staffId
            };

            attendanceRecords.Add(attendance);

            if (status == AttendanceStatus.Absent)
            {
                var student = await _context.Students.FindAsync(record.StudentId);
                if (student != null)
                {
                    absences.Add((student.Id, student.FullName));
                }
            }
        }

        _context.Attendances.AddRange(attendanceRecords);
        await _context.SaveChangesAsync();

        // Send SMS notifications for absences
        if (request.SendNotifications && absences.Any())
        {
            await SendAbsenceNotificationsAsync(absences, request.Date);
        }

        return Ok(new { Message = $"{attendanceRecords.Count} attendance records saved" });
    }

    /// <summary>
    /// Get attendance summary for a student
    /// </summary>
    [HttpGet("student/{studentId:guid}/summary")]
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

                // Prefer WhatsApp when configured, otherwise fall back to SMS.
                if (_whatsAppService.IsConfigured)
                {
                    await _whatsAppService.SendMessageAsync(guardian.Phone, message);
                }
                else
                {
                    await _smsService.SendSmsAsync(guardian.Phone, message);
                }
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
