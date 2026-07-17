using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Timetable.Commands;

public record UpdateClassroomCommand : IRequest<Result>
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Building { get; init; }
    public int Capacity { get; init; }
    public bool HasProjector { get; init; }
    public bool HasWhiteboard { get; init; }
    public bool IsActive { get; init; }
}
