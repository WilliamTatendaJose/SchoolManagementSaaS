using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Staff.Commands;

public record UpdateStaffCommand : IRequest<Result>
{
    public Guid Id { get; init; }
    public string? Department { get; init; }
    public string? JobTitle { get; init; }
    public DateTime? DateOfJoining { get; init; }
    public string? Qualifications { get; init; }
    public string? Specialization { get; init; }
    public bool IsTeacher { get; init; }
    public bool IsActive { get; init; }
}
