using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;

namespace SMS.Application.Features.Boarding.Commands;

public class RequestWeekendLeaveCommandHandler : IRequestHandler<RequestWeekendLeaveCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public RequestWeekendLeaveCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(RequestWeekendLeaveCommand request, CancellationToken cancellationToken)
    {
        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.Id == request.StudentId, cancellationToken);

        if (student == null)
        {
            return Result<Guid>.Failure("Student not found");
        }

        if (student.DormitoryId == null)
        {
            return Result<Guid>.Failure("Weekend leave can only be requested for a boarding student");
        }

        if (request.ExpectedReturnDate < request.DepartureDate)
        {
            return Result<Guid>.Failure("Expected return cannot be before departure");
        }

        var leave = new WeekendLeave
        {
            StudentId = request.StudentId,
            DepartureDate = request.DepartureDate,
            ExpectedReturnDate = request.ExpectedReturnDate,
            Destination = request.Destination,
            Reason = request.Reason,
            Status = WeekendLeaveStatuses.Pending
        };

        _context.WeekendLeaves.Add(leave);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(leave.Id);
    }
}
