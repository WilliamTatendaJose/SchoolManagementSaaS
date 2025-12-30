using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Academic.Queries;

public class GetSubjectsQueryHandler : IRequestHandler<GetSubjectsQuery, Result<List<SubjectDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetSubjectsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<SubjectDto>>> Handle(GetSubjectsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Subjects
            .AsNoTracking()
            .Include(s => s.Teachers)
            .Include(s => s.Classes)
            .AsQueryable();

        if (request.ActiveOnly == true)
        {
            query = query.Where(s => s.IsActive);
        }

        if (request.CoreOnly == true)
        {
            query = query.Where(s => s.IsCore);
        }

        var subjects = await query
            .OrderBy(s => s.Name)
            .Select(s => new SubjectDto
            {
                Id = s.Id,
                Name = s.Name,
                Code = s.Code,
                Description = s.Description,
                IsCore = s.IsCore,
                IsActive = s.IsActive,
                TeacherCount = s.Teachers.Count,
                ClassCount = s.Classes.Count
            })
            .ToListAsync(cancellationToken);

        return Result<List<SubjectDto>>.Success(subjects);
    }
}
