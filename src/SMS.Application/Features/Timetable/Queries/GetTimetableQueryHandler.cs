using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Timetable.Queries;

public class GetTimetableQueryHandler : IRequestHandler<GetTimetableQuery, Result<List<TimetableSlotDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetTimetableQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<TimetableSlotDto>>> Handle(GetTimetableQuery request, CancellationToken cancellationToken)
    {
        var query = _context.TimetableSlots
            .AsNoTracking()
            .Include(s => s.Class)
            .Include(s => s.Subject)
            .Include(s => s.Teacher)
                .ThenInclude(t => t.User)
            .Include(s => s.Classroom)
            .Where(s => s.AcademicTermId == request.AcademicTermId);

        if (request.ClassId.HasValue)
        {
            query = query.Where(s => s.ClassId == request.ClassId.Value);
        }

        if (request.TeacherId.HasValue)
        {
            query = query.Where(s => s.TeacherId == request.TeacherId.Value);
        }

        if (request.ClassroomId.HasValue)
        {
            query = query.Where(s => s.ClassroomId == request.ClassroomId.Value);
        }

        var slots = await query
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .Select(s => new TimetableSlotDto
            {
                Id = s.Id,
                ClassId = s.ClassId,
                ClassName = s.Class.Name,
                SubjectId = s.SubjectId,
                SubjectName = s.Subject.Name,
                TeacherId = s.TeacherId,
                TeacherName = s.Teacher.User.FirstName + " " + s.Teacher.User.LastName,
                ClassroomId = s.ClassroomId,
                ClassroomName = s.Classroom != null ? s.Classroom.Name : null,
                DayOfWeek = s.DayOfWeek.ToString(),
                StartTime = s.StartTime,
                EndTime = s.EndTime
            })
            .ToListAsync(cancellationToken);

        return Result<List<TimetableSlotDto>>.Success(slots);
    }
}
