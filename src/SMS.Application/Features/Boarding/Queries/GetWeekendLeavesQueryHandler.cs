using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Boarding.Queries;

public class GetWeekendLeavesQueryHandler : IRequestHandler<GetWeekendLeavesQuery, Result<PaginatedList<WeekendLeaveDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetWeekendLeavesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<WeekendLeaveDto>>> Handle(GetWeekendLeavesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.WeekendLeaves
            .AsNoTracking()
            .Include(l => l.Student)
                .ThenInclude(s => s.Dormitory)
            .AsQueryable();

        if (request.StudentId.HasValue)
        {
            query = query.Where(l => l.StudentId == request.StudentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(l => l.Status == request.Status);
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(l => l.DepartureDate >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(l => l.DepartureDate <= request.ToDate.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(l => l.DepartureDate)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(l => new WeekendLeaveDto
            {
                Id = l.Id,
                StudentId = l.StudentId,
                StudentName = l.Student.FullName,
                DormitoryName = l.Student.Dormitory != null ? l.Student.Dormitory.Name : null,
                DepartureDate = l.DepartureDate,
                ExpectedReturnDate = l.ExpectedReturnDate,
                Destination = l.Destination,
                Reason = l.Reason,
                Status = l.Status,
                ReviewNote = l.ReviewNote,
                CollectedBy = l.CollectedBy,
                ActualDepartureAt = l.ActualDepartureAt,
                ActualReturnAt = l.ActualReturnAt,
                GuardianNotified = l.GuardianNotified
            })
            .ToListAsync(cancellationToken);

        var paginated = new PaginatedList<WeekendLeaveDto>(items, totalCount, request.PageNumber, request.PageSize);

        return Result<PaginatedList<WeekendLeaveDto>>.Success(paginated);
    }
}
