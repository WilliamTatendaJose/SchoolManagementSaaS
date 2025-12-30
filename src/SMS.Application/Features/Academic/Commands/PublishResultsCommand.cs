using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Academic.Commands;

public record PublishResultsCommand : IRequest<Result>
{
    public Guid AssessmentId { get; init; }
}
