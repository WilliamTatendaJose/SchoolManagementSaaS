using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;

namespace SMS.Application.Features.Library.Commands;

public class BorrowBookCommandHandler : IRequestHandler<BorrowBookCommand, Result<Guid>>
{
    private static readonly TimeSpan DefaultLoanPeriod = TimeSpan.FromDays(14);

    private readonly IApplicationDbContext _context;

    public BorrowBookCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(BorrowBookCommand request, CancellationToken cancellationToken)
    {
        var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == request.BookId, cancellationToken);
        if (book == null)
        {
            return Result<Guid>.Failure("Book not found");
        }

        if (!book.IsActive)
        {
            return Result<Guid>.Failure("Book is not available for loan");
        }

        var studentExists = await _context.Students.AnyAsync(s => s.Id == request.StudentId, cancellationToken);
        if (!studentExists)
        {
            return Result<Guid>.Failure("Student not found");
        }

        var activeLoans = await _context.BookLoans
            .CountAsync(l => l.BookId == book.Id && l.Status == BookLoanStatuses.Borrowed, cancellationToken);

        if (activeLoans >= book.TotalCopies)
        {
            return Result<Guid>.Failure("No copies of this book are currently available");
        }

        var alreadyBorrowed = await _context.BookLoans
            .AnyAsync(l => l.BookId == book.Id && l.StudentId == request.StudentId && l.Status == BookLoanStatuses.Borrowed, cancellationToken);

        if (alreadyBorrowed)
        {
            return Result<Guid>.Failure("This student already has a copy of this book on loan");
        }

        var borrowedDate = request.BorrowedDate ?? DateTime.UtcNow;

        var loan = new BookLoan
        {
            BookId = book.Id,
            StudentId = request.StudentId,
            BorrowedDate = borrowedDate,
            DueDate = request.DueDate ?? borrowedDate.Add(DefaultLoanPeriod),
            Status = BookLoanStatuses.Borrowed
        };

        _context.BookLoans.Add(loan);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(loan.Id);
    }
}
