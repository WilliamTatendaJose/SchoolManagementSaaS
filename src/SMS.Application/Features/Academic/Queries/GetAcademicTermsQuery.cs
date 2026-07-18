using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Academic.Queries;

public record GetAcademicTermsQuery : IRequest<Result<List<AcademicTermDto>>>
{
    public Guid? AcademicYearId { get; init; }
}

public record AcademicTermDto
{
    public Guid Id { get; init; }
    public Guid AcademicYearId { get; init; }
    public string AcademicYearName { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int TermNumber { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public bool IsCurrent { get; init; }
}
