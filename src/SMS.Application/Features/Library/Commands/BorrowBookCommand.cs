using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Library.Commands;

public record BorrowBookCommand : IRequest<Result<Guid>>
{
    public Guid BookId { get; init; }
    public Guid StudentId { get; init; }
    public DateTime? BorrowedDate { get; init; }
    public DateTime? DueDate { get; init; }
}
