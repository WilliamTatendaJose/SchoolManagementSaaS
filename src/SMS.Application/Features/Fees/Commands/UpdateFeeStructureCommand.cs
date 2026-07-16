using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Fees.Commands;

/// <summary>
/// Command to update an existing fee structure
/// </summary>
public record UpdateFeeStructureCommand : IRequest<Result>
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Amount { get; init; }
    public string FeeType { get; init; } = string.Empty;
    public bool IsRecurring { get; init; }
    public bool IsOptional { get; init; }
}
