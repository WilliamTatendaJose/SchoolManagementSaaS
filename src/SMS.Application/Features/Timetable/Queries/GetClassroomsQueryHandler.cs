using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Timetable.Queries;

public class GetClassroomsQueryHandler : IRequestHandler<GetClassroomsQuery, Result<List<ClassroomDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetClassroomsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<ClassroomDto>>> Handle(GetClassroomsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Classrooms.AsNoTracking().AsQueryable();

        if (request.ActiveOnly == true)
        {
            query = query.Where(c => c.IsActive);
        }

        var classrooms = await query
            .OrderBy(c => c.Name)
            .Select(c => new ClassroomDto
            {
                Id = c.Id,
                Name = c.Name,
                Building = c.Building,
                Capacity = c.Capacity,
                HasProjector = c.HasProjector,
                HasWhiteboard = c.HasWhiteboard,
                IsActive = c.IsActive
            })
            .ToListAsync(cancellationToken);

        return Result<List<ClassroomDto>>.Success(classrooms);
    }
}
