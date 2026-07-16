using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Enrollments.Commands;

/// <summary>
/// Command to transfer a student's active enrollment to a different class/stream
/// within the same academic year
/// </summary>
public record TransferStudentCommand : IRequest<Result>
{
    public Guid StudentId { get; init; }
    public Guid AcademicYearId { get; init; }
    public Guid ToClassId { get; init; }
    public Guid? ToStreamId { get; init; }
}
