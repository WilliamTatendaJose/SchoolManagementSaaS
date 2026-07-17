using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Assets.Commands;

public class UpdateAssetCommandHandler : IRequestHandler<UpdateAssetCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdateAssetCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(UpdateAssetCommand request, CancellationToken cancellationToken)
    {
        var asset = await _context.Assets.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if (asset == null)
        {
            return Result.Failure("Asset not found");
        }

        if (request.AssignedToId.HasValue
            && !await _context.Staff.AnyAsync(s => s.Id == request.AssignedToId.Value, cancellationToken))
        {
            return Result.Failure("Assigned staff member not found");
        }

        // Stamp the assignment date when the asset is (re)assigned.
        if (request.AssignedToId != asset.AssignedToId)
        {
            asset.AssignedDate = request.AssignedToId.HasValue ? DateTime.UtcNow : null;
        }

        asset.Name = request.Name;
        asset.Category = request.Category;
        asset.Description = request.Description;
        asset.Location = request.Location;
        asset.PurchasePrice = request.PurchasePrice;
        asset.PurchaseDate = request.PurchaseDate;
        asset.Condition = request.Condition;
        asset.AssignedToId = request.AssignedToId;
        asset.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
