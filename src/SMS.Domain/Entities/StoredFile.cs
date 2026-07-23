using SMS.Domain.Common;

namespace SMS.Domain.Entities;

/// <summary>
/// A file stored directly in the database (Postgres <c>bytea</c>) rather than in an
/// external object store. Backs <c>IFileStorageService</c> so uploads (assignment
/// attachments, submissions, course materials) work with no cloud dependency. Served
/// only through the authenticated, tenant-scoped <c>FilesController</c>.
/// </summary>
public class StoredFile : TenantEntity
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long Length { get; set; }
    public byte[] Content { get; set; } = [];

    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
}
