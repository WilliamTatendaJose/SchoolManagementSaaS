using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Hostel.Queries;

public class GetDormitoriesQueryHandler : IRequestHandler<GetDormitoriesQuery, Result<List<DormitoryDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetDormitoriesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<DormitoryDto>>> Handle(GetDormitoriesQuery request, CancellationToken cancellationToken)
    {
        var dormitories = await _context.Dormitories
            .AsNoTracking()
            .Include(d => d.Warden)
                .ThenInclude(w => w!.User)
            .OrderBy(d => d.Name)
            .Select(d => new
            {
                d.Id,
                d.Name,
                d.Capacity,
                d.Gender,
                d.WardenId,
                WardenName = d.Warden != null ? d.Warden.User.FirstName + " " + d.Warden.User.LastName : null,
                d.IsActive,
                Occupants = _context.Students.Count(s => s.DormitoryId == d.Id)
            })
            .ToListAsync(cancellationToken);

        var result = dormitories
            .Select(d => new DormitoryDto
            {
                Id = d.Id,
                Name = d.Name,
                Capacity = d.Capacity,
                Gender = d.Gender,
                WardenId = d.WardenId,
                WardenName = d.WardenName,
                IsActive = d.IsActive,
                Occupants = d.Occupants,
                AvailableBeds = Math.Max(0, d.Capacity - d.Occupants)
            })
            .ToList();

        return Result<List<DormitoryDto>>.Success(result);
    }
}
