using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Academic.Commands;

public class DeleteAcademicYearCommandHandler : IRequestHandler<DeleteAcademicYearCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public DeleteAcademicYearCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(DeleteAcademicYearCommand request, CancellationToken cancellationToken)
    {
        var academicYear = await _context.AcademicYears
            .FirstOrDefaultAsync(y => y.Id == request.Id, cancellationToken);

        if (academicYear == null)
        {
            return Result.Failure("Academic year not found");
        }

        var hasEnrollments = await _context.Enrollments
            .AnyAsync(e => e.AcademicYearId == request.Id, cancellationToken);

        if (hasEnrollments)
        {
            return Result.Failure("Cannot delete an academic year that has student enrollments.");
        }

        _context.AcademicYears.Remove(academicYear);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
