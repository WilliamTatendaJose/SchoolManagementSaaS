using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Lms.Commands;

public record GradeAssignmentSubmissionCommand : IRequest<Result>
{
    public Guid SubmissionId { get; init; }
    public decimal Grade { get; init; }
    public string? Feedback { get; init; }
}
