using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Features.Library;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Library.Queries;

public class GetBooksQueryHandler : IRequestHandler<GetBooksQuery, Result<List<BookDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetBooksQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<BookDto>>> Handle(GetBooksQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Books.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(b => b.Title.ToLower().Contains(term) || b.Author.ToLower().Contains(term));
        }

        var books = await query
            .OrderBy(b => b.Title)
            .Select(b => new
            {
                b.Id,
                b.Title,
                b.Author,
                b.Isbn,
                b.Category,
                b.TotalCopies,
                b.IsActive,
                OnLoan = b.Loans.Count(l => l.Status == BookLoanStatuses.Borrowed)
            })
            .ToListAsync(cancellationToken);

        var result = books
            .Select(b => new BookDto
            {
                Id = b.Id,
                Title = b.Title,
                Author = b.Author,
                Isbn = b.Isbn,
                Category = b.Category,
                TotalCopies = b.TotalCopies,
                OnLoan = b.OnLoan,
                AvailableCopies = Math.Max(0, b.TotalCopies - b.OnLoan),
                IsActive = b.IsActive
            })
            .ToList();

        return Result<List<BookDto>>.Success(result);
    }
}
