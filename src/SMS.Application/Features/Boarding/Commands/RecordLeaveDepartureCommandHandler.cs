using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Boarding.Commands;

public class RecordLeaveDepartureCommandHandler : IRequestHandler<RecordLeaveDepartureCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public RecordLeaveDepartureCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(RecordLeaveDepartureCommand request, CancellationToken cancellationToken)
    {
        var leave = await _context.WeekendLeaves.FirstOrDefaultAsync(l => l.Id == request.LeaveId, cancellationToken);

        if (leave == null)
        {
            return Result.Failure("Leave request not found");
        }

        if (leave.Status != WeekendLeaveStatuses.Approved)
        {
            return Result.Failure($"Only approved leave can be signed out (current status: {leave.Status})");
        }

        leave.Status = WeekendLeaveStatuses.Departed;
        leave.CollectedBy = request.CollectedBy;
        leave.ActualDepartureAt = request.DepartedAt ?? DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
