using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Fees.Commands;

public class DeleteFeeStructureCommandHandler : IRequestHandler<DeleteFeeStructureCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public DeleteFeeStructureCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(DeleteFeeStructureCommand request, CancellationToken cancellationToken)
    {
        var feeStructure = await _context.FeeStructures
            .FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken);

        if (feeStructure == null)
        {
            return Result.Failure("Fee structure not found");
        }

        _context.FeeStructures.Remove(feeStructure);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
