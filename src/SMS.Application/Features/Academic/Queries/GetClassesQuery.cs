using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Academic.Queries;

public record GetClassesQuery : IRequest<Result<List<ClassDto>>>
{
    public bool IncludeStudentCount { get; init; }
}

public record ClassDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Code { get; init; }
    public int Level { get; init; }
    public int Capacity { get; init; }
    public int StudentCount { get; init; }
    public string? ClassTeacherName { get; init; }
    public string? ClassroomName { get; init; }
    public List<StreamDto> Streams { get; init; } = [];
}

public record StreamDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Capacity { get; init; }
    public int StudentCount { get; init; }
}
