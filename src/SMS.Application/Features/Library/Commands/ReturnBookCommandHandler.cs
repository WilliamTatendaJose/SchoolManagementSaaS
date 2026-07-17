using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Interfaces;
using Result = SMS.Application.Common.Models.Result;

namespace SMS.Application.Features.Library.Commands;

public class ReturnBookCommandHandler : IRequestHandler<ReturnBookCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public ReturnBookCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(ReturnBookCommand request, CancellationToken cancellationToken)
    {
        var loan = await _context.BookLoans
            .Include(l => l.Book)
            .FirstOrDefaultAsync(l => l.Id == request.LoanId, cancellationToken);

        if (loan == null)
        {
            return Result.Failure("Loan not found");
        }

        if (loan.Status != BookLoanStatuses.Borrowed)
        {
            return Result.Failure($"Only an active loan can be closed (current status: {loan.Status})");
        }

        loan.Status = request.Lost ? BookLoanStatuses.Lost : BookLoanStatuses.Returned;
        loan.ReturnedDate = request.ReturnedDate ?? DateTime.UtcNow;

        if (request.Lost)
        {
            // The physical copy is gone, so it no longer counts toward the catalog total.
            loan.Book.TotalCopies = Math.Max(0, loan.Book.TotalCopies - 1);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
