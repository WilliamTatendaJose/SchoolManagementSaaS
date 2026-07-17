using MediatR;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;

namespace SMS.Application.Features.Library.Commands;

public class CreateBookCommandHandler : IRequestHandler<CreateBookCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateBookCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateBookCommand request, CancellationToken cancellationToken)
    {
        var book = new Book
        {
            Title = request.Title,
            Author = request.Author,
            Isbn = request.Isbn,
            Category = request.Category,
            TotalCopies = request.TotalCopies,
            IsActive = true
        };

        _context.Books.Add(book);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(book.Id);
    }
}
