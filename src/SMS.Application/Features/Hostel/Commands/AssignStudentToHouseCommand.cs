using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Hostel.Commands;

/// <summary>
/// Assigns a student to a house. Pass a null HouseId to un-assign.
/// </summary>
public record AssignStudentToHouseCommand : IRequest<Result>
{
    public Guid StudentId { get; init; }
    public Guid? HouseId { get; init; }
}
