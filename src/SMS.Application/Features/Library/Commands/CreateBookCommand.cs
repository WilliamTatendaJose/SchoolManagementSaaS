using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Library.Commands;

public record CreateBookCommand : IRequest<Result<Guid>>
{
    public string Title { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string? Isbn { get; init; }
    public string? Category { get; init; }
    public int TotalCopies { get; init; }
}
