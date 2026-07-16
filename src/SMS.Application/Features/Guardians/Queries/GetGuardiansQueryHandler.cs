using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Guardians.Queries;

public class GetGuardiansQueryHandler : IRequestHandler<GetGuardiansQuery, Result<PaginatedList<GuardianListDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetGuardiansQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<GuardianListDto>>> Handle(GetGuardiansQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Guardians
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(g =>
                g.FirstName.ToLower().Contains(searchTerm) ||
                g.LastName.ToLower().Contains(searchTerm) ||
                (g.Phone != null && g.Phone.ToLower().Contains(searchTerm)) ||
                (g.Email != null && g.Email.ToLower().Contains(searchTerm)) ||
                (g.NationalId != null && g.NationalId.ToLower().Contains(searchTerm)));
        }

        if (request.StudentId.HasValue)
        {
            query = query.Where(g => g.Students.Any(sg => sg.StudentId == request.StudentId.Value));
        }

        query = request.SortBy?.ToLower() switch
        {
            "firstname" => request.SortDescending ? query.OrderByDescending(g => g.FirstName) : query.OrderBy(g => g.FirstName),
            "lastname" => request.SortDescending ? query.OrderByDescending(g => g.LastName) : query.OrderBy(g => g.LastName),
            _ => query.OrderBy(g => g.LastName).ThenBy(g => g.FirstName)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var guardians = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(g => new GuardianListDto
            {
                Id = g.Id,
                FirstName = g.FirstName,
                LastName = g.LastName,
                FullName = g.FirstName + " " + g.LastName,
                Gender = g.Gender.ToString(),
                NationalId = g.NationalId,
                Phone = g.Phone,
                Email = g.Email,
                Occupation = g.Occupation,
                StudentCount = g.Students.Count
            })
            .ToListAsync(cancellationToken);

        var paginatedList = new PaginatedList<GuardianListDto>(guardians, totalCount, request.PageNumber, request.PageSize);

        return Result<PaginatedList<GuardianListDto>>.Success(paginatedList);
    }
}
