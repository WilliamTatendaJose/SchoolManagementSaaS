using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Academic.Queries;

public record GetAcademicYearsQuery : IRequest<Result<List<AcademicYearDto>>>;

public record AcademicYearDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Year { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public bool IsCurrent { get; init; }
    public int TermCount { get; init; }
}
