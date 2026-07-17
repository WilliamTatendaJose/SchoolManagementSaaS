using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.ParentPortal.Queries;

/// <summary>Returns the signed-in parent's children.</summary>
public record GetMyChildrenQuery : IRequest<Result<List<MyChildDto>>>;

public record MyChildDto
{
    public Guid StudentId { get; init; }
    public string StudentNumber { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? ClassName { get; init; }
    public decimal OutstandingBalance { get; init; }
}
