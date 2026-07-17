using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Common.Staffing;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Staff.Commands;

public class ReviewLeaveRequestCommandHandler : IRequestHandler<ReviewLeaveRequestCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ReviewLeaveRequestCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(ReviewLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        var leaveRequest = await _context.LeaveRequests
            .FirstOrDefaultAsync(l => l.Id == request.LeaveRequestId, cancellationToken);

        if (leaveRequest == null)
        {
            return Result.Failure("Leave request not found");
        }

        if (leaveRequest.Status != LeaveStatuses.Pending)
        {
            return Result.Failure($"Leave request has already been {leaveRequest.Status.ToLowerInvariant()}");
        }

        var approverStaffId = await _context.Staff
            .Where(s => s.UserId == _currentUser.UserId)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync(cancellationToken);

        leaveRequest.Status = request.Approve ? LeaveStatuses.Approved : LeaveStatuses.Rejected;
        leaveRequest.ApprovedById = approverStaffId;
        leaveRequest.ApprovedAt = DateTime.UtcNow;
        leaveRequest.ApproverComments = request.Comments;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
