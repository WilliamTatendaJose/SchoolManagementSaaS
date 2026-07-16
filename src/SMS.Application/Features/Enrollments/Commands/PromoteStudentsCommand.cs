using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Enrollments.Commands;

/// <summary>
/// Command to bulk-promote a set of students into a target class for a new academic year.
/// Any existing active enrollments for each student are deactivated and a new active
/// enrollment is created.
/// </summary>
public record PromoteStudentsCommand : IRequest<Result<PromotionResultDto>>
{
    public Guid ToClassId { get; init; }
    public Guid? ToStreamId { get; init; }
    public Guid ToAcademicYearId { get; init; }
    public DateTime? EnrollmentDate { get; init; }
    public List<Guid> StudentIds { get; init; } = [];
}

public record PromotionResultDto
{
    public int PromotedCount { get; init; }
    public List<Guid> SkippedStudentIds { get; init; } = [];
}
