using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Academic.Commands;

public record CreateAssessmentCommand : IRequest<Result<Guid>>
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid SubjectId { get; init; }
    public Guid ClassId { get; init; }
    public Guid AcademicTermId { get; init; }
    public string AssessmentType { get; init; } = string.Empty;
    public decimal MaxScore { get; init; }
    public decimal WeightPercentage { get; init; }
    public DateTime? Date { get; init; }
}
