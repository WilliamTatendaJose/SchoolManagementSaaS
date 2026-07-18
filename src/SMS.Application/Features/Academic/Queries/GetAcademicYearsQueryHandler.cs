using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Academic.Queries;

public class GetAcademicYearsQueryHandler : IRequestHandler<GetAcademicYearsQuery, Result<List<AcademicYearDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetAcademicYearsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<AcademicYearDto>>> Handle(GetAcademicYearsQuery request, CancellationToken cancellationToken)
    {
        var years = await _context.AcademicYears
            .AsNoTracking()
            .Include(y => y.Terms)
            .OrderByDescending(y => y.StartDate)
            .Select(y => new AcademicYearDto
            {
                Id = y.Id,
                Name = y.Name,
                Year = y.Year,
                StartDate = y.StartDate,
                EndDate = y.EndDate,
                IsCurrent = y.IsCurrent,
                TermCount = y.Terms.Count
            })
            .ToListAsync(cancellationToken);

        return Result<List<AcademicYearDto>>.Success(years);
    }
}
