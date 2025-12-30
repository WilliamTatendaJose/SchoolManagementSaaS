using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Attendance.Queries;

public class GetClassAttendanceQueryHandler : IRequestHandler<GetClassAttendanceQuery, Result<List<ClassAttendanceDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetClassAttendanceQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<ClassAttendanceDto>>> Handle(GetClassAttendanceQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Attendances
            .AsNoTracking()
            .Include(a => a.Student)
            .Where(a => a.ClassId == request.ClassId && a.Date.Date == request.Date.Date);

        if (request.SubjectId.HasValue)
        {
            query = query.Where(a => a.SubjectId == request.SubjectId.Value);
        }

        var attendance = await query
            .Select(a => new ClassAttendanceDto
            {
                Id = a.Id,
                StudentId = a.StudentId,
                StudentNumber = a.Student.StudentNumber,
                StudentName = a.Student.FullName,
                Status = a.Status.ToString(),
                TimeIn = a.TimeIn,
                Reason = a.Reason,
                SmsNotificationSent = a.SmsNotificationSent
            })
            .OrderBy(a => a.StudentName)
            .ToListAsync(cancellationToken);

        return Result<List<ClassAttendanceDto>>.Success(attendance);
    }
}
