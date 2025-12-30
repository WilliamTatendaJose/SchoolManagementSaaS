using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;

namespace SMS.Application.Features.Academic.Commands;

public class CreateAcademicYearCommandHandler : IRequestHandler<CreateAcademicYearCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateAcademicYearCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateAcademicYearCommand request, CancellationToken cancellationToken)
    {
        // If setting as current, unset existing current year
        if (request.SetAsCurrent)
        {
            var currentYear = await _context.AcademicYears
                .FirstOrDefaultAsync(y => y.IsCurrent, cancellationToken);
            
            if (currentYear != null)
            {
                currentYear.IsCurrent = false;
            }
        }

        var academicYear = new AcademicYear
        {
            Name = request.Name,
            Year = request.Year,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsCurrent = request.SetAsCurrent
        };

        // Add terms
        foreach (var termDto in request.Terms.OrderBy(t => t.TermNumber))
        {
            academicYear.Terms.Add(new AcademicTerm
            {
                Name = termDto.Name,
                TermNumber = termDto.TermNumber,
                StartDate = termDto.StartDate,
                EndDate = termDto.EndDate,
                IsCurrent = false
            });
        }

        _context.AcademicYears.Add(academicYear);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(academicYear.Id);
    }
}
