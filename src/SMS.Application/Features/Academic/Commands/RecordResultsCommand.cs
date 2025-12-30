using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Academic.Commands;

public record RecordResultsCommand : IRequest<Result<int>>
{
    public Guid AssessmentId { get; init; }
    public List<StudentResultDto> Results { get; init; } = [];
}

public record StudentResultDto
{
    public Guid StudentId { get; init; }
    public decimal Score { get; init; }
    public string? Comment { get; init; }
}
