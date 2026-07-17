using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Library.Queries;

/// <summary>Lists the book catalog, optionally filtered by a title/author search term.</summary>
public record GetBooksQuery : IRequest<Result<List<BookDto>>>
{
    public string? Search { get; init; }
}

public record BookDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string? Isbn { get; init; }
    public string? Category { get; init; }
    public int TotalCopies { get; init; }
    public int OnLoan { get; init; }
    public int AvailableCopies { get; init; }
    public bool IsActive { get; init; }
}
