using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Fees.Commands;

/// <summary>
/// Command to create a fee structure for a class in an academic year
/// </summary>
public record CreateFeeStructureCommand : IRequest<Result<Guid>>
{
    public Guid ClassId { get; init; }
    public Guid AcademicYearId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Amount { get; init; }
    public string FeeType { get; init; } = string.Empty;
    public bool IsRecurring { get; init; }
    public bool IsOptional { get; init; }
}
