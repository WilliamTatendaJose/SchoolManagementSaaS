using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Academic.Queries;

public record GetSubjectsQuery : IRequest<Result<List<SubjectDto>>>
{
    public bool? ActiveOnly { get; init; }
    public bool? CoreOnly { get; init; }
}

public record SubjectDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsCore { get; init; }
    public bool IsActive { get; init; }
    public int TeacherCount { get; init; }
    public int ClassCount { get; init; }
}
