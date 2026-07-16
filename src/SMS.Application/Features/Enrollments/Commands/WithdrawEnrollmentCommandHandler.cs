using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Enrollments.Commands;

public class WithdrawEnrollmentCommandHandler : IRequestHandler<WithdrawEnrollmentCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public WithdrawEnrollmentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(WithdrawEnrollmentCommand request, CancellationToken cancellationToken)
    {
        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.Id == request.EnrollmentId, cancellationToken);

        if (enrollment == null)
        {
            return Result.Failure("Enrollment not found");
        }

        if (!enrollment.IsActive)
        {
            return Result.Failure("Enrollment is already withdrawn");
        }

        enrollment.IsActive = false;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
