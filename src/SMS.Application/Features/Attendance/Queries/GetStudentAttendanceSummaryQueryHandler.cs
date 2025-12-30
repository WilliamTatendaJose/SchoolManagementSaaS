using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Enums;

namespace SMS.Application.Features.Attendance.Queries;

public class GetStudentAttendanceSummaryQueryHandler : IRequestHandler<GetStudentAttendanceSummaryQuery, Result<AttendanceSummaryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetStudentAttendanceSummaryQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<AttendanceSummaryDto>> Handle(GetStudentAttendanceSummaryQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Attendances
            .AsNoTracking()
            .Where(a => a.StudentId == request.StudentId);

        // Apply date filters
        if (request.AcademicTermId.HasValue)
        {
            var term = await _context.AcademicTerms.FindAsync([request.AcademicTermId.Value], cancellationToken);
            if (term != null)
            {
                query = query.Where(a => a.Date >= term.StartDate && a.Date <= term.EndDate);
            }
        }
        else
        {
            if (request.StartDate.HasValue)
            {
                query = query.Where(a => a.Date >= request.StartDate.Value);
            }
            if (request.EndDate.HasValue)
            {
                query = query.Where(a => a.Date <= request.EndDate.Value);
            }
        }

        var records = await query.ToListAsync(cancellationToken);

        var totalDays = records.Count;
        var presentDays = records.Count(r => r.Status == AttendanceStatus.Present);
        var absentDays = records.Count(r => r.Status == AttendanceStatus.Absent);
        var lateDays = records.Count(r => r.Status == AttendanceStatus.Late);
        var excusedDays = records.Count(r => r.Status == AttendanceStatus.Excused);

        var attendancePercentage = totalDays > 0
            ? Math.Round((decimal)(presentDays + lateDays) / totalDays * 100, 2)
            : 0;

        var summary = new AttendanceSummaryDto
        {
            TotalDays = totalDays,
            PresentDays = presentDays,
            AbsentDays = absentDays,
            LateDays = lateDays,
            ExcusedDays = excusedDays,
            AttendancePercentage = attendancePercentage
        };

        return Result<AttendanceSummaryDto>.Success(summary);
    }
}
