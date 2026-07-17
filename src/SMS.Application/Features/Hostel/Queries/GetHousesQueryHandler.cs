using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Hostel.Queries;

public class GetHousesQueryHandler : IRequestHandler<GetHousesQuery, Result<List<HouseDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetHousesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<HouseDto>>> Handle(GetHousesQuery request, CancellationToken cancellationToken)
    {
        var houses = await _context.Houses
            .AsNoTracking()
            .Include(h => h.HouseMaster)
                .ThenInclude(m => m!.User)
            .OrderBy(h => h.Name)
            .Select(h => new HouseDto
            {
                Id = h.Id,
                Name = h.Name,
                Color = h.Color,
                HouseMasterId = h.HouseMasterId,
                HouseMasterName = h.HouseMaster != null ? h.HouseMaster.User.FirstName + " " + h.HouseMaster.User.LastName : null,
                MemberCount = _context.Students.Count(s => s.HouseId == h.Id)
            })
            .ToListAsync(cancellationToken);

        return Result<List<HouseDto>>.Success(houses);
    }
}
