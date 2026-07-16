using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Fees.Queries;

/// <summary>
/// Query to get fee structures, optionally filtered by class and/or academic year
/// </summary>
public record GetFeeStructuresQuery : IRequest<Result<List<FeeStructureDto>>>
{
    public Guid? ClassId { get; init; }
    public Guid? AcademicYearId { get; init; }
}

public record FeeStructureDto
{
    public Guid Id { get; init; }
    public Guid ClassId { get; init; }
    public string ClassName { get; init; } = string.Empty;
    public Guid AcademicYearId { get; init; }
    public string AcademicYearName { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Amount { get; init; }
    public string FeeType { get; init; } = string.Empty;
    public bool IsRecurring { get; init; }
    public bool IsOptional { get; init; }
}
