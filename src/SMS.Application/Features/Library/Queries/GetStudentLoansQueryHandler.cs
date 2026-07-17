using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Library.Queries;

public class GetStudentLoansQueryHandler : IRequestHandler<GetStudentLoansQuery, Result<List<BookLoanDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetStudentLoansQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<BookLoanDto>>> Handle(GetStudentLoansQuery request, CancellationToken cancellationToken)
    {
        if (!await _context.Students.AnyAsync(s => s.Id == request.StudentId, cancellationToken))
        {
            return Result<List<BookLoanDto>>.Failure("Student not found");
        }

        var loans = await _context.BookLoans
            .AsNoTracking()
            .Include(l => l.Book)
            .Include(l => l.Student)
            .Where(l => l.StudentId == request.StudentId)
            .OrderByDescending(l => l.BorrowedDate)
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
