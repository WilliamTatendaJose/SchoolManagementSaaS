using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Files.Queries;

/// <summary>Fetch a stored file's bytes for download. Tenant-scoped by the global query filter.</summary>
public record GetStoredFileQuery(Guid Id) : IRequest<Result<StoredFileDto>>;

public record StoredFileDto
{
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = "application/octet-stream";
    public byte[] Content { get; init; } = [];
}
