using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Academic.Queries;

public class GetClassesQueryHandler : IRequestHandler<GetClassesQuery, Result<List<ClassDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetClassesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<ClassDto>>> Handle(GetClassesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Classes
            .AsNoTracking()
            .Include(c => c.ClassTeacher)
                .ThenInclude(t => t!.User)
            .Include(c => c.Classroom)
            .Include(c => c.Streams)
            .Include(c => c.Students)
            .OrderBy(c => c.Level)
            .ThenBy(c => c.Name);

        var classes = await query
            .Select(c => new ClassDto
            {
                Id = c.Id,
                Name = c.Name,
                Code = c.Code,
                Level = c.Level,
                Capacity = c.Capacity,
                StudentCount = c.Students.Count,
                ClassTeacherName = c.ClassTeacher != null ? c.ClassTeacher.User.FullName : null,
                ClassroomName = c.Classroom != null ? c.Classroom.Name : null,
                Streams = c.Streams.Select(s => new StreamDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Capacity = s.Capacity,
                    StudentCount = s.Enrollments.Count(e => e.IsActive)
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        return Result<List<ClassDto>>.Success(classes);
    }
}
