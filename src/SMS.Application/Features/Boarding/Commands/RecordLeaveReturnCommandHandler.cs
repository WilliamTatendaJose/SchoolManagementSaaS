using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Boarding.Commands;

public class RecordLeaveReturnCommandHandler : IRequestHandler<RecordLeaveReturnCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public RecordLeaveReturnCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(RecordLeaveReturnCommand request, CancellationToken cancellationToken)
    {
        var leave = await _context.WeekendLeaves.FirstOrDefaultAsync(l => l.Id == request.LeaveId, cancellationToken);

        if (leave == null)
        {
            return Result.Failure("Leave request not found");
        }

        if (leave.Status != WeekendLeaveStatuses.Departed)
        {
            return Result.Failure($"Only a departed student can be signed back in (current status: {leave.Status})");
        }

        leave.Status = WeekendLeaveStatuses.Returned;
        leave.ActualReturnAt = request.ReturnedAt ?? DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
