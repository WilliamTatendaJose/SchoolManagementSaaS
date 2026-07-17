using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Hostel.Queries;

/// <summary>Lists the students currently allocated to a dormitory.</summary>
public record GetDormitoryOccupantsQuery(Guid DormitoryId) : IRequest<Result<List<DormitoryOccupantDto>>>;

public record DormitoryOccupantDto
{
    public Guid StudentId { get; init; }
    public string StudentNumber { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? ClassName { get; init; }
}
