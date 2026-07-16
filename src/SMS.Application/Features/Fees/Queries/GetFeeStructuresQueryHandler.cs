using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Fees.Queries;

public class GetFeeStructuresQueryHandler : IRequestHandler<GetFeeStructuresQuery, Result<List<FeeStructureDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetFeeStructuresQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<FeeStructureDto>>> Handle(GetFeeStructuresQuery request, CancellationToken cancellationToken)
    {
        var query = _context.FeeStructures
            .AsNoTracking()
            .Include(f => f.Class)
            .Include(f => f.AcademicYear)
            .AsQueryable();

        if (request.ClassId.HasValue)
        {
            query = query.Where(f => f.ClassId == request.ClassId.Value);
        }

        if (request.AcademicYearId.HasValue)
        {
            query = query.Where(f => f.AcademicYearId == request.AcademicYearId.Value);
        }

        var feeStructures = await query
            .OrderBy(f => f.Class.Name)
            .ThenBy(f => f.Name)
            .Select(f => new FeeStructureDto
            {
                Id = f.Id,
                ClassId = f.ClassId,
                ClassName = f.Class.Name,
                AcademicYearId = f.AcademicYearId,
                AcademicYearName = f.AcademicYear.Name,
                Name = f.Name,
                Description = f.Description,
                Amount = f.Amount,
                FeeType = f.FeeType,
                IsRecurring = f.IsRecurring,
                IsOptional = f.IsOptional
            })
            .ToListAsync(cancellationToken);

        return Result<List<FeeStructureDto>>.Success(feeStructures);
    }
}
