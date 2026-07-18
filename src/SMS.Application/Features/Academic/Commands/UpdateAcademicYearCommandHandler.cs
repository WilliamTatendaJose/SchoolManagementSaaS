using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Academic.Commands;

public class UpdateAcademicYearCommandHandler : IRequestHandler<UpdateAcademicYearCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdateAcademicYearCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(UpdateAcademicYearCommand request, CancellationToken cancellationToken)
    {
        var academicYear = await _context.AcademicYears
            .FirstOrDefaultAsync(y => y.Id == request.Id, cancellationToken);

        if (academicYear == null)
        {
            return Result.Failure("Academic year not found");
        }

        if (request.SetAsCurrent && !academicYear.IsCurrent)
        {
            var currentYear = await _context.AcademicYears
                .FirstOrDefaultAsync(y => y.IsCurrent && y.Id != request.Id, cancellationToken);

            if (currentYear != null)
            {
                currentYear.IsCurrent = false;
            }
        }

        academicYear.Name = request.Name;
        academicYear.Year = request.Year;
        academicYear.StartDate = request.StartDate;
        academicYear.EndDate = request.EndDate;
        academicYear.IsCurrent = request.SetAsCurrent;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
