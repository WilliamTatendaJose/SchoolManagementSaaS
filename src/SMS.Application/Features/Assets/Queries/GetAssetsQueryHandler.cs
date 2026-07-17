using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Assets.Queries;

public class GetAssetsQueryHandler : IRequestHandler<GetAssetsQuery, Result<PaginatedList<AssetDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetAssetsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<AssetDto>>> Handle(GetAssetsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Assets
            .AsNoTracking()
            .Include(a => a.AssignedTo)
                .ThenInclude(s => s!.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(a => a.Name.ToLower().Contains(term) || a.AssetNumber.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            query = query.Where(a => a.Category == request.Category);
        }

        if (!string.IsNullOrWhiteSpace(request.Condition))
        {
            query = query.Where(a => a.Condition == request.Condition);
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(a => a.IsActive == request.IsActive.Value);
        }

        query = query.OrderBy(a => a.AssetNumber);

        var totalCount = await query.CountAsync(cancellationToken);

        var assets = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new AssetDto
            {
                Id = a.Id,
                AssetNumber = a.AssetNumber,
                Name = a.Name,
                Category = a.Category,
                Location = a.Location,
                PurchasePrice = a.PurchasePrice,
                Condition = a.Condition,
                AssignedToId = a.AssignedToId,
                AssignedToName = a.AssignedTo != null ? a.AssignedTo.User.FirstName + " " + a.AssignedTo.User.LastName : null,
                IsActive = a.IsActive
            })
            .ToListAsync(cancellationToken);

        var paginatedList = new PaginatedList<AssetDto>(assets, totalCount, request.PageNumber, request.PageSize);

        return Result<PaginatedList<AssetDto>>.Success(paginatedList);
    }
}
