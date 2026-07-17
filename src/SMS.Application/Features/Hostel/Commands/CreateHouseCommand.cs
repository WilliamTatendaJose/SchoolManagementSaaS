using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Hostel.Commands;

public record CreateHouseCommand : IRequest<Result<Guid>>
{
    public string Name { get; init; } = string.Empty;
    public string? Color { get; init; }
    public Guid? HouseMasterId { get; init; }
}
