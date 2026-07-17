using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Staff.Queries;

public class GetStaffQueryHandler : IRequestHandler<GetStaffQuery, Result<PaginatedList<StaffListDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetStaffQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<StaffListDto>>> Handle(GetStaffQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Staff
            .AsNoTracking()
            .Include(s => s.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(s =>
                s.StaffNumber.ToLower().Contains(term) ||
                s.User.FirstName.ToLower().Contains(term) ||
                s.User.LastName.ToLower().Contains(term) ||
                s.User.Email.ToLower().Contains(term));
        }

        if (request.IsTeacher.HasValue)
        {
            query = query.Where(s => s.IsTeacher == request.IsTeacher.Value);
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(s => s.IsActive == request.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Department))
        {
            query = query.Where(s => s.Department == request.Department);
        }

        query = query.OrderBy(s => s.User.LastName).ThenBy(s => s.User.FirstName);

        var totalCount = await query.CountAsync(cancellationToken);

        var staff = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new StaffListDto
            {
                Id = s.Id,
                StaffNumber = s.StaffNumber,
                FullName = s.User.FirstName + " " + s.User.LastName,
                Email = s.User.Email,
                Department = s.Department,
                JobTitle = s.JobTitle,
                IsTeacher = s.IsTeacher,
                IsActive = s.IsActive
            })
            .ToListAsync(cancellationToken);

        var paginatedList = new PaginatedList<StaffListDto>(staff, totalCount, request.PageNumber, request.PageSize);

        return Result<PaginatedList<StaffListDto>>.Success(paginatedList);
    }
}
