using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Hostel.Queries;

public record GetDormitoriesQuery : IRequest<Result<List<DormitoryDto>>>;

public record DormitoryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Capacity { get; init; }
    public string? Gender { get; init; }
    public Guid? WardenId { get; init; }
    public string? WardenName { get; init; }
    public bool IsActive { get; init; }
    public int Occupants { get; init; }
    public int AvailableBeds { get; init; }
}
