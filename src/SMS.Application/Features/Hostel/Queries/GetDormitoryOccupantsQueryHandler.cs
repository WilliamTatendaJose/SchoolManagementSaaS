using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Hostel.Queries;

public class GetDormitoryOccupantsQueryHandler : IRequestHandler<GetDormitoryOccupantsQuery, Result<List<DormitoryOccupantDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetDormitoryOccupantsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<DormitoryOccupantDto>>> Handle(GetDormitoryOccupantsQuery request, CancellationToken cancellationToken)
    {
        if (!await _context.Dormitories.AnyAsync(d => d.Id == request.DormitoryId, cancellationToken))
        {
            return Result<List<DormitoryOccupantDto>>.Failure("Dormitory not found");
        }

        var occupants = await _context.Students
            .AsNoTracking()
            .Include(s => s.CurrentClass)
            .Where(s => s.DormitoryId == request.DormitoryId)
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .Select(s => new DormitoryOccupantDto
            {
                StudentId = s.Id,
                StudentNumber = s.StudentNumber,
                FullName = s.FirstName + " " + s.LastName,
                ClassName = s.CurrentClass != null ? s.CurrentClass.Name : null
            })
            .ToListAsync(cancellationToken);

        return Result<List<DormitoryOccupantDto>>.Success(occupants);
    }
}
