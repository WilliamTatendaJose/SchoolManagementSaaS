using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Staff.Queries;

public class GetLeaveRequestsQueryHandler : IRequestHandler<GetLeaveRequestsQuery, Result<List<LeaveRequestDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetLeaveRequestsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<LeaveRequestDto>>> Handle(GetLeaveRequestsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.LeaveRequests
            .AsNoTracking()
            .Include(l => l.Staff)
                .ThenInclude(s => s.User)
            .AsQueryable();

        if (request.StaffId.HasValue)
        {
            query = query.Where(l => l.StaffId == request.StaffId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(l => l.Status == request.Status);
        }

        var leaveRequests = await query
            .OrderByDescending(l => l.StartDate)
            .Select(l => new LeaveRequestDto
            {
                Id = l.Id,
                StaffId = l.StaffId,
                StaffName = l.Staff.User.FirstName + " " + l.Staff.User.LastName,
                LeaveType = l.LeaveType,
                StartDate = l.StartDate,
                EndDate = l.EndDate,
                NumberOfDays = (l.EndDate - l.StartDate).Days + 1,
                Status = l.Status,
                Reason = l.Reason,
                ApproverComments = l.ApproverComments,
                ApprovedAt = l.ApprovedAt
            })
            .ToListAsync(cancellationToken);

        return Result<List<LeaveRequestDto>>.Success(leaveRequests);
    }
}
