using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Configuration;
using Wiley.Apartments.Web.Data;
using Wiley.Apartments.Web.Services;

namespace Wiley.Apartments.Tests.Services;

public class DocumentServiceTests
{
    private sealed class FixedClock : Wiley.Apartments.Contracts.IDateTimeService
    {
        public DateTime UtcNow { get; set; } = new(2026, 8, 9, 18, 0, 0, DateTimeKind.Utc);
        public DateTime ToDisplayTime(DateTime utc) => utc;
        public DateTime ToUtc(DateTime local) => DateTime.SpecifyKind(local, DateTimeKind.Utc);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    private static (ApartmentsDbContext Db, DocumentService Service, string Root) Create()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApartmentsDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new ApartmentsDbContext(options);
        db.Database.EnsureCreated();
        var root = Path.Combine(Path.GetTempPath(), "clerksuite-docs-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var env = new TestHostEnvironment();
        var service = new DocumentService(
            db,
            Options.Create(new ClerkSuiteOptions { DocumentRoot = root }),
            env,
            new FixedClock(),
            NullLogger<DocumentService>.Instance);
        return (db, service, root);
    }

    [Fact]
    public async Task UploadAsync_WritesFileAndMetadata()
    {
        var (db, service, root) = Create();
        await using (db)
        {
            var entityId = Guid.NewGuid();
            await using var content = new MemoryStream("hello"u8.ToArray());
            var info = await service.UploadAsync(
                DocumentEntityType.Tenant,
                entityId,
                DocumentCategory.Screening,
                "packet.pdf",
                "application/pdf",
                content,
                "clerk",
                "uploads/tenant-test");

            info.FilePathOnNas.Should().StartWith("uploads/tenant-test/");
            File.Exists(Path.Combine(root, info.FilePathOnNas.Replace('/', Path.DirectorySeparatorChar)))
                .Should().BeTrue();
            (await service.ListForEntityAsync(DocumentEntityType.Tenant, entityId)).Should().ContainSingle();
            (await service.ReadBytesAsync(info.Id)).Should().Equal("hello"u8.ToArray());
        }

        try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
    }

    [Fact]
    public async Task UploadAsync_RejectsDisallowedExtension()
    {
        var (db, service, root) = Create();
        await using (db)
        {
            await using var content = new MemoryStream([1, 2, 3]);
            var act = () => service.UploadAsync(
                DocumentEntityType.Unit,
                Guid.NewGuid(),
                DocumentCategory.Other,
                "notes.exe",
                "application/octet-stream",
                content,
                "clerk",
                "uploads/bad");

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not allowed*");
        }

        try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
    }
}
