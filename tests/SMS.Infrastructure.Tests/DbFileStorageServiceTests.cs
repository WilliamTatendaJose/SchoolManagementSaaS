using FluentAssertions;
using SMS.Application.Features.Files.Queries;
using SMS.Domain.Entities;
using SMS.Infrastructure.Services;
using Xunit;

namespace SMS.Infrastructure.Tests;

/// <summary>
/// DB-backed file storage (replaces S3). Round-trips bytes through the database and stays
/// tenant-scoped: a file uploaded in one tenant is invisible to another.
/// </summary>
public class DbFileStorageServiceTests : IAsyncLifetime
{
    private readonly TenantTestContext _harness = new();
    private Guid _tenantA;
    private Guid _tenantB;

    public async Task InitializeAsync()
    {
        var a = new Tenant { Name = "Tenant A", Code = "TA" };
        var b = new Tenant { Name = "Tenant B", Code = "TB" };
        await using var db = _harness.CreateDbContext();
        db.Tenants.AddRange(a, b);
        await db.SaveChangesAsync();
        _tenantA = a.Id;
        _tenantB = b.Id;
    }

    public Task DisposeAsync()
    {
        _harness.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Upload_then_download_round_trips_the_bytes()
    {
        _harness.UseTenant(_tenantA);
        var bytes = new byte[] { 1, 2, 3, 4, 5 };

        string key;
        await using (var db = _harness.CreateDbContext())
        {
            key = await new DbFileStorageService(db).UploadAsync(new MemoryStream(bytes), "notes.pdf", "application/pdf");
        }

        Guid.TryParse(key, out _).Should().BeTrue("the key is the StoredFile id");

        await using (var db = _harness.CreateDbContext())
        {
            var stream = await new DbFileStorageService(db).DownloadAsync(key);
            stream.Should().NotBeNull();
            using var ms = new MemoryStream();
            await stream!.CopyToAsync(ms);
            ms.ToArray().Should().Equal(bytes);
        }
    }

    [Fact]
    public void GetFileUrl_points_at_the_authenticated_download_endpoint()
    {
        _harness.UseTenant(_tenantA);
        using var db = _harness.CreateDbContext();
        var id = Guid.NewGuid().ToString();
        new DbFileStorageService(db).GetFileUrl(id).Should().Be($"/api/files/{id}");
    }

    [Fact]
    public async Task A_file_uploaded_in_one_tenant_is_not_downloadable_from_another()
    {
        _harness.UseTenant(_tenantA);
        string key;
        await using (var db = _harness.CreateDbContext())
        {
            key = await new DbFileStorageService(db).UploadAsync(new MemoryStream([9, 9, 9]), "secret.txt", "text/plain");
        }

        _harness.UseTenant(_tenantB);
        await using (var db = _harness.CreateDbContext())
        {
            (await new DbFileStorageService(db).DownloadAsync(key)).Should().BeNull();
        }
    }

    [Fact]
    public async Task GetStoredFileQuery_returns_the_file_for_the_owning_tenant()
    {
        _harness.UseTenant(_tenantA);
        string key;
        await using (var db = _harness.CreateDbContext())
        {
            key = await new DbFileStorageService(db).UploadAsync(new MemoryStream([7, 7]), "a.bin", "application/octet-stream");
        }

        await using (var db = _harness.CreateDbContext())
        {
            var result = await new GetStoredFileQueryHandler(db).Handle(new GetStoredFileQuery(Guid.Parse(key)), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.Data!.FileName.Should().Be("a.bin");
            result.Data.Content.Should().Equal([7, 7]);
        }

        _harness.UseTenant(_tenantB);
        await using (var db = _harness.CreateDbContext())
        {
            var result = await new GetStoredFileQueryHandler(db).Handle(new GetStoredFileQuery(Guid.Parse(key)), CancellationToken.None);
            result.IsSuccess.Should().BeFalse("the file belongs to another tenant");
        }
    }
}
