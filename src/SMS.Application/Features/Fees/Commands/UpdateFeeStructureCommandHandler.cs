using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Fees.Commands;

public class UpdateFeeStructureCommandHandler : IRequestHandler<UpdateFeeStructureCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdateFeeStructureCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(UpdateFeeStructureCommand request, CancellationToken cancellationToken)
    {
        var feeStructure = await _context.FeeStructures
            .FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken);

        if (feeStructure == null)
        {
            return Result.Failure("Fee structure not found");
        }

        feeStructure.Name = request.Name;
        feeStructure.Description = request.Description;
        feeStructure.Amount = request.Amount;
        feeStructure.FeeType = request.FeeType;
        feeStructure.IsRecurring = request.IsRecurring;
        feeStructure.IsOptional = request.IsOptional;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
