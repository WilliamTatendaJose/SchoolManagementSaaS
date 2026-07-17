using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;
using SMS.Domain.Enums;

namespace SMS.Application.Features.Attendance.Commands;

public class MarkAttendanceCommandHandler : IRequestHandler<MarkAttendanceCommand, Result<int>>
{
    private readonly IApplicationDbContext _context;
    private readonly ISmsService _smsService;
    private readonly IWhatsAppService _whatsAppService;
    private readonly ICurrentUserService _currentUserService;

    public MarkAttendanceCommandHandler(
        IApplicationDbContext context,
        ISmsService smsService,
        IWhatsAppService whatsAppService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _smsService = smsService;
        _whatsAppService = whatsAppService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<int>> Handle(MarkAttendanceCommand request, CancellationToken cancellationToken)
    {
        var staffId = _currentUserService.UserId;
        var attendanceRecords = new List<Domain.Entities.Attendance>();
        var absences = new List<(Guid StudentId, string StudentName)>();

        foreach (var record in request.Records)
        {
            var status = Enum.Parse<AttendanceStatus>(record.Status);

            // Check if attendance already exists for this student on this date
            var existingAttendance = await _context.Attendances
                .FirstOrDefaultAsync(a =>
                    a.StudentId == record.StudentId &&
                    a.ClassId == request.ClassId &&
                    a.Date.Date == request.Date.Date &&
                    a.SubjectId == request.SubjectId,
                    cancellationToken);

            if (existingAttendance != null)
            {
                // Update existing record
                existingAttendance.Status = status;
                existingAttendance.TimeIn = record.TimeIn;
                existingAttendance.Reason = record.Reason;
                existingAttendance.MarkedById = staffId;
            }
            else
            {
                // Create new record
                var attendance = new Domain.Entities.Attendance
                {
                    StudentId = record.StudentId,
                    ClassId = request.ClassId,
                    SubjectId = request.SubjectId,
                    TimetableSlotId = request.TimetableSlotId,
                    Date = request.Date,
                    Status = status,
                    TimeIn = record.TimeIn,
                    Reason = record.Reason,
                    MarkedById = staffId
                };

                attendanceRecords.Add(attendance);
            }

            if (status == AttendanceStatus.Absent)
            {
                var student = await _context.Students.FindAsync([record.StudentId], cancellationToken);
                if (student != null)
                {
                    absences.Add((student.Id, student.FullName));
                }
            }
        }

        if (attendanceRecords.Count > 0)
        {
            _context.Attendances.AddRange(attendanceRecords);
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Send SMS notifications for absences
        if (request.SendNotifications && absences.Count > 0)
        {
            await SendAbsenceNotificationsAsync(absences, request.Date, cancellationToken);
        }

        return Result<int>.Success(request.Records.Count);
    }

    private async Task SendAbsenceNotificationsAsync(
        List<(Guid StudentId, string StudentName)> absences,
        DateTime date,
        CancellationToken cancellationToken)
    {
        foreach (var (studentId, studentName) in absences)
        {
            var guardian = await _context.StudentGuardians
                .Where(sg => sg.StudentId == studentId && sg.IsPrimaryContact)
                .Include(sg => sg.Guardian)
                .Select(sg => sg.Guardian)
                .FirstOrDefaultAsync(cancellationToken);

            if (guardian?.Phone != null)
            {
                var message = $"Dear Parent/Guardian, {studentName} was marked absent on {date:dd/MM/yyyy}. Please contact the school if this is unexpected.";

                // Prefer WhatsApp when configured, otherwise fall back to SMS.
                if (_whatsAppService.IsConfigured)
                {
                    await _whatsAppService.SendMessageAsync(guardian.Phone, message, cancellationToken);
                }
                else
                {
                    await _smsService.SendSmsAsync(guardian.Phone, message, cancellationToken);
                }

                // Update attendance record to mark notification as sent
                var attendance = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.StudentId == studentId && a.Date.Date == date.Date, cancellationToken);

                if (attendance != null)
                {
                    attendance.SmsNotificationSent = true;
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
