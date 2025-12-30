namespace SMS.Application.Interfaces;

/// <summary>
/// Interface for file storage operations
/// </summary>
public interface IFileStorageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<Stream?> DownloadAsync(string fileKey, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string fileKey, CancellationToken cancellationToken = default);
    string GetFileUrl(string fileKey);
}
