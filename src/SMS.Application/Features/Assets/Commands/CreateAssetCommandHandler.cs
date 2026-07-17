using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;

namespace SMS.Application.Features.Assets.Commands;

public class CreateAssetCommandHandler : IRequestHandler<CreateAssetCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateAssetCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateAssetCommand request, CancellationToken cancellationToken)
    {
        if (request.AssignedToId.HasValue
            && !await _context.Staff.AnyAsync(s => s.Id == request.AssignedToId.Value, cancellationToken))
        {
            return Result<Guid>.Failure("Assigned staff member not found");
        }

        var asset = new Asset
        {
            AssetNumber = await GenerateAssetNumberAsync(cancellationToken),
            Name = request.Name,
            Category = request.Category,
            Description = request.Description,
            Location = request.Location,
            PurchasePrice = request.PurchasePrice,
            PurchaseDate = request.PurchaseDate,
            Condition = request.Condition,
            AssignedToId = request.AssignedToId,
            AssignedDate = request.AssignedToId.HasValue ? DateTime.UtcNow : null,
            IsActive = true
        };

        _context.Assets.Add(asset);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(asset.Id);
    }

    private async Task<string> GenerateAssetNumberAsync(CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var count = await _context.Assets.CountAsync(a => a.AssetNumber.StartsWith($"AST-{year}-"), cancellationToken) + 1;
        return $"AST-{year}-{count:D5}";
    }
}
