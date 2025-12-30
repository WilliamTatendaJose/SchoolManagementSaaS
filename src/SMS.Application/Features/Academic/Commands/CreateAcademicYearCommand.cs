using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Academic.Commands;

public record CreateAcademicYearCommand : IRequest<Result<Guid>>
{
    public string Name { get; init; } = string.Empty;
    public int Year { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public bool SetAsCurrent { get; init; }
    public List<TermDto> Terms { get; init; } = [];
}

public record TermDto
{
    public string Name { get; init; } = string.Empty;
    public int TermNumber { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
}
