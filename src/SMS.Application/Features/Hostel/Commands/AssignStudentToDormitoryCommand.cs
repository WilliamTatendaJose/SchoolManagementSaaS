using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Hostel.Commands;

/// <summary>
/// Allocates a boarding student to a dormitory. Pass a null DormitoryId to un-assign.
/// </summary>
public record AssignStudentToDormitoryCommand : IRequest<Result>
{
    public Guid StudentId { get; init; }
    public Guid? DormitoryId { get; init; }
}
