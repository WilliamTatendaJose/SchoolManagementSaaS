using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Enums;

namespace SMS.Application.Features.Dashboard.Queries;

public class GetDashboardQueryHandler : IRequestHandler<GetDashboardQuery, Result<DashboardDto>>
{
    private readonly IApplicationDbContext _context;

    public GetDashboardQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<DashboardDto>> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        var activeStudents = await _context.Students.CountAsync(s => s.Status == StudentStatus.Active, cancellationToken);
        var boardingStudents = await _context.Students.CountAsync(s => s.DormitoryId != null, cancellationToken);
        var totalStaff = await _context.Staff.CountAsync(s => s.IsActive, cancellationToken);
        var totalClasses = await _context.Classes.CountAsync(cancellationToken);

        // Finance (summed in memory to avoid provider-specific decimal SQL).
        var invoiceRows = await _context.Invoices
            .Select(i => new { i.TotalAmount, i.DiscountAmount, i.PaidAmount })
            .ToListAsync(cancellationToken);

        var totalBilled = invoiceRows.Sum(i => i.TotalAmount - i.DiscountAmount);
        var totalCollected = invoiceRows.Sum(i => i.PaidAmount);
        var totalOutstanding = totalBilled - totalCollected;
        var collectionRate = totalBilled > 0 ? Math.Round(totalCollected / totalBilled * 100, 2) : 0m;

        // Attendance rate over the last 30 days.
        var since = DateTime.UtcNow.Date.AddDays(-30);
        var recent = await _context.Attendances
            .Where(a => a.Date >= since)
            .Select(a => a.Status)
            .ToListAsync(cancellationToken);

        var attendanceRate = recent.Count > 0
            ? (int)Math.Round(
                (double)recent.Count(s => s == AttendanceStatus.Present || s == AttendanceStatus.Late) / recent.Count * 100)
            : 0;

        var enrollmentByClass = await _context.Classes
            .AsNoTracking()
            .OrderBy(c => c.Level).ThenBy(c => c.Name)
            .Select(c => new ClassEnrollmentDto
            {
                ClassName = c.Name,
                StudentCount = _context.Students.Count(s => s.CurrentClassId == c.Id && s.Status == StudentStatus.Active)
            })
            .ToListAsync(cancellationToken);

        var dto = new DashboardDto
        {
            ActiveStudents = activeStudents,
            BoardingStudents = boardingStudents,
            TotalStaff = totalStaff,
            TotalClasses = totalClasses,
            TotalBilled = totalBilled,
            TotalCollected = totalCollected,
            TotalOutstanding = totalOutstanding,
            CollectionRatePercent = collectionRate,
            AttendanceRatePercent = attendanceRate,
            EnrollmentByClass = enrollmentByClass
        };

        return Result<DashboardDto>.Success(dto);
    }
}
