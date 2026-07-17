using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Common.Staffing;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;

namespace SMS.Application.Features.Staff.Commands;

public class CreateLeaveRequestCommandHandler : IRequestHandler<CreateLeaveRequestCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateLeaveRequestCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        if (!await _context.Staff.AnyAsync(s => s.Id == request.StaffId, cancellationToken))
        {
            return Result<Guid>.Failure("Staff not found");
        }

        var leaveRequest = new LeaveRequest
        {
            StaffId = request.StaffId,
            LeaveType = request.LeaveType,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Reason = request.Reason,
            Status = LeaveStatuses.Pending
        };

        _context.LeaveRequests.Add(leaveRequest);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(leaveRequest.Id);
    }
}
