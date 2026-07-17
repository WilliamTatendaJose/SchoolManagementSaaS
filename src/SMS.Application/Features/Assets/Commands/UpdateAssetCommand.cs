using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Assets.Commands;

public record UpdateAssetCommand : IRequest<Result>
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Location { get; init; }
    public decimal? PurchasePrice { get; init; }
    public DateTime? PurchaseDate { get; init; }
    public string Condition { get; init; } = "Good";
    public Guid? AssignedToId { get; init; }
    public bool IsActive { get; init; }
}
