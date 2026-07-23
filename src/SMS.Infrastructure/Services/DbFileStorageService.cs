using Microsoft.EntityFrameworkCore;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;
using Stream = System.IO.Stream;

namespace SMS.Infrastructure.Services;

/// <summary>
/// Stores uploaded files as rows in the database (Postgres <c>bytea</c>) instead of an
/// external object store, so the app has no cloud/AWS dependency. The returned "key" is the
/// <see cref="StoredFile"/> id; <see cref="GetFileUrl"/> points at the authenticated,
/// tenant-scoped <c>FilesController</c> download endpoint.
/// </summary>
public class DbFileStorageService : IFileStorageService
{
    private readonly IApplicationDbContext _context;

    public DbFileStorageService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        using var memory = new MemoryStream();
        await fileStream.CopyToAsync(memory, cancellationToken);
        var bytes = memory.ToArray();

        var file = new StoredFile
        {
            FileName = fileName,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            Length = bytes.Length,
            Content = bytes
        };

        _context.StoredFiles.Add(file);
        // Persist now so the caller gets a usable key even though the surrounding unit of
        // work may save again later (the file row is independent of what references it).
        await _context.SaveChangesAsync(cancellationToken);

        return file.Id.ToString();
    }

    public async Task<Stream?> DownloadAsync(string fileKey, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(fileKey, out var id))
        {
            return null;
        }

        var content = await _context.StoredFiles
            .AsNoTracking()
            .Where(f => f.Id == id)
            .Select(f => f.Content)
            .FirstOrDefaultAsync(cancellationToken);

        return content == null ? null : new MemoryStream(content);
    }

    public async Task<bool> DeleteAsync(string fileKey, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(fileKey, out var id))
        {
            return false;
        }

        var file = await _context.StoredFiles.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (file == null)
        {
            return false;
        }

        _context.StoredFiles.Remove(file);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public string GetFileUrl(string fileKey) => $"/api/files/{fileKey}";
}
