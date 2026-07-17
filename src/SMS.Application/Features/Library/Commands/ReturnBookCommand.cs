using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Library.Commands;

/// <summary>Closes an active loan, either as returned or (with <see cref="Lost"/>) as lost.</summary>
public record ReturnBookCommand : IRequest<Result>
{
    public Guid LoanId { get; init; }
    public DateTime? ReturnedDate { get; init; }
    public bool Lost { get; init; }
}
