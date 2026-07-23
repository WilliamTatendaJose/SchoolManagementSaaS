using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Files.Queries;

public class GetStoredFileQueryHandler : IRequestHandler<GetStoredFileQuery, Result<StoredFileDto>>
{
    private readonly IApplicationDbContext _context;

    public GetStoredFileQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<StoredFileDto>> Handle(GetStoredFileQuery request, CancellationToken cancellationToken)
    {
        // The global tenant filter scopes this to the caller's tenant, so a file id from
        // another tenant simply isn't found.
        var file = await _context.StoredFiles
            .AsNoTracking()
            .Where(f => f.Id == request.Id)
            .Select(f => new StoredFileDto
            {
                FileName = f.FileName,
                ContentType = f.ContentType,
                Content = f.Content
            })
            .FirstOrDefaultAsync(cancellationToken);

        return file == null
            ? Result<StoredFileDto>.Failure("File not found")
            : Result<StoredFileDto>.Success(file);
    }
}
