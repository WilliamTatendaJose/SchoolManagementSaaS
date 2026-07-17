using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Library.Queries;

public class GetOverdueLoansQueryHandler : IRequestHandler<GetOverdueLoansQuery, Result<List<BookLoanDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetOverdueLoansQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<BookLoanDto>>> Handle(GetOverdueLoansQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var loans = await _context.BookLoans
            .AsNoTracking()
            .Include(l => l.Book)
            .Include(l => l.Student)
            .Where(l => l.Status == BookLoanStatuses.Borrowed && l.DueDate < now)
            .OrderBy(l => l.DueDate)
            .Select(l => new BookLoanDto
            {
                Id = l.Id,
                BookId = l.BookId,
                BookTitle = l.Book.Title,
                StudentId = l.StudentId,
                StudentName = l.Student.FirstName + " " + l.Student.LastName,
                BorrowedDate = l.BorrowedDate,
                DueDate = l.DueDate,
                ReturnedDate = l.ReturnedDate,
                Status = l.Status
            })
            .ToListAsync(cancellationToken);

        return Result<List<BookLoanDto>>.Success(loans);
    }
}
