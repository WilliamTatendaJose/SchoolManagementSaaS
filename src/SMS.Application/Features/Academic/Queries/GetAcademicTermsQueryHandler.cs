using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Academic.Queries;

public class GetAcademicTermsQueryHandler : IRequestHandler<GetAcademicTermsQuery, Result<List<AcademicTermDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetAcademicTermsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<AcademicTermDto>>> Handle(GetAcademicTermsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.AcademicTerms
            .AsNoTracking()
            .Include(t => t.AcademicYear)
            .AsQueryable();

        if (request.AcademicYearId.HasValue)
        {
            query = query.Where(t => t.AcademicYearId == request.AcademicYearId.Value);
        }

        var terms = await query
            .OrderByDescending(t => t.StartDate)
            .ThenBy(t => t.TermNumber)
            .Select(t => new AcademicTermDto
            {
                Id = t.Id,
                AcademicYearId = t.AcademicYearId,
                AcademicYearName = t.AcademicYear.Name,
                Name = t.Name,
                TermNumber = t.TermNumber,
                StartDate = t.StartDate,
                EndDate = t.EndDate,
                IsCurrent = t.IsCurrent,
            })
            .ToListAsync(cancellationToken);

        return Result<List<AcademicTermDto>>.Success(terms);
    }
}
