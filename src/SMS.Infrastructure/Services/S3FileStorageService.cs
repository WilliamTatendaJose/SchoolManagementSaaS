using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using SMS.Application.Interfaces;

namespace SMS.Infrastructure.Services;

/// <summary>
/// AWS S3 file storage service implementation
/// </summary>
public class S3FileStorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly string _baseUrl;

    public S3FileStorageService(IAmazonS3 s3Client, IConfiguration configuration)
    {
        _s3Client = s3Client;
        _bucketName = configuration["AWS:S3:BucketName"] ?? throw new InvalidOperationException("S3 bucket name not configured");
        _baseUrl = configuration["AWS:S3:BaseUrl"] ?? $"https://{_bucketName}.s3.amazonaws.com";
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var key = $"{Guid.NewGuid()}/{fileName}";

        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = fileStream,
            ContentType = contentType
        };

        await _s3Client.PutObjectAsync(request, cancellationToken);

        return key;
    }

    public async Task<Stream?> DownloadAsync(string fileKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = fileKey
            };

            var response = await _s3Client.GetObjectAsync(request, cancellationToken);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<bool> DeleteAsync(string fileKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = fileKey
            };

            await _s3Client.DeleteObjectAsync(request, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public string GetFileUrl(string fileKey)
    {
        return $"{_baseUrl}/{fileKey}";
    }
}
