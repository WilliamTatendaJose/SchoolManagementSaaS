using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Enrollments.Commands;

/// <summary>
/// Command to enroll a student into a class for an academic year
/// </summary>
public record EnrollStudentCommand : IRequest<Result<Guid>>
{
    public Guid StudentId { get; init; }
    public Guid ClassId { get; init; }
    public Guid? StreamId { get; init; }
    public Guid AcademicYearId { get; init; }
    public DateTime? EnrollmentDate { get; init; }
}
