using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Assets.Queries;

public record GetAssetsQuery : IRequest<Result<PaginatedList<AssetDto>>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? SearchTerm { get; init; }
    public string? Category { get; init; }
    public string? Condition { get; init; }
    public bool? IsActive { get; init; }
}

public record AssetDto
{
    public Guid Id { get; init; }
    public string AssetNumber { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Location { get; init; }
    public decimal? PurchasePrice { get; init; }
    public DateTime? PurchaseDate { get; init; }
    public string Condition { get; init; } = string.Empty;
    public Guid? AssignedToId { get; init; }
    public string? AssignedToName { get; init; }
    public bool IsActive { get; init; }
}
