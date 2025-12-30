using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Academic.Commands;

public record CreateClassCommand : IRequest<Result<Guid>>
{
    public string Name { get; init; } = string.Empty;
    public string? Code { get; init; }
    public int Level { get; init; }
    public int Capacity { get; init; }
    public Guid? ClassTeacherId { get; init; }
    public Guid? ClassroomId { get; init; }
}
