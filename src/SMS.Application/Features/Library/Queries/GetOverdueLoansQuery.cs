using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Library.Queries;

/// <summary>Lists active loans whose due date has passed.</summary>
public record GetOverdueLoansQuery : IRequest<Result<List<BookLoanDto>>>;

public record BookLoanDto
{
    public Guid Id { get; init; }
    public Guid BookId { get; init; }
    public string BookTitle { get; init; } = string.Empty;
    public Guid StudentId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public DateTime BorrowedDate { get; init; }
    public DateTime DueDate { get; init; }
    public DateTime? ReturnedDate { get; init; }
    public string Status { get; init; } = string.Empty;
}
