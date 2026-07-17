using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Hostel.Commands;

public record CreateDormitoryCommand : IRequest<Result<Guid>>
{
    public string Name { get; init; } = string.Empty;
    public int Capacity { get; init; }
    public string? Gender { get; init; }
    public Guid? WardenId { get; init; }
}
