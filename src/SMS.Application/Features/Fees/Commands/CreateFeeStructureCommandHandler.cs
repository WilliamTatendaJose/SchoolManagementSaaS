using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;

namespace SMS.Application.Features.Fees.Commands;

public class CreateFeeStructureCommandHandler : IRequestHandler<CreateFeeStructureCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateFeeStructureCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateFeeStructureCommand request, CancellationToken cancellationToken)
    {
        if (!await _context.Classes.AnyAsync(c => c.Id == request.ClassId, cancellationToken))
        {
            return Result<Guid>.Failure("Class not found");
        }

        if (!await _context.AcademicYears.AnyAsync(y => y.Id == request.AcademicYearId, cancellationToken))
        {
            return Result<Guid>.Failure("Academic year not found");
        }

        var feeStructure = new FeeStructure
        {
            ClassId = request.ClassId,
            AcademicYearId = request.AcademicYearId,
            Name = request.Name,
            Description = request.Description,
            Amount = request.Amount,
            FeeType = request.FeeType,
            IsRecurring = request.IsRecurring,
            IsOptional = request.IsOptional
        };

        _context.FeeStructures.Add(feeStructure);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(feeStructure.Id);
    }
}
