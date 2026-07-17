using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Academic.Commands;

/// <summary>
/// Upserts the class-teacher and/or head-teacher remarks for a student's report card in a
/// term. Fields left null are unchanged from whatever was previously saved.
/// </summary>
public record SaveReportCardCommentCommand : IRequest<Result>
{
    public Guid StudentId { get; init; }
    public Guid AcademicTermId { get; init; }
    public string? ClassTeacherComment { get; init; }
    public string? HeadComment { get; init; }
}
