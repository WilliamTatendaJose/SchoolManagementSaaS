using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Hostel.Queries;

public record GetHousesQuery : IRequest<Result<List<HouseDto>>>;

public record HouseDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Color { get; init; }
    public Guid? HouseMasterId { get; init; }
    public string? HouseMasterName { get; init; }
    public int MemberCount { get; init; }
}
