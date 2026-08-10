using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Tests.Support;
using Wiley.Apartments.Web.Data;
using Wiley.Apartments.Web.Services;

namespace Wiley.Apartments.Tests.Services;

public sealed class DocumentVaultMetadataSyncTests
{
    [Fact]
    public async Task SoftDeleteMatchingAsync_MarksDocumentDeleted()
    {
        using var dbFactory = new SqliteTestDatabase();
        await using var db = dbFactory.CreateContext();
        db.Documents.Add(new Document
        {
            Id = Guid.NewGuid(),
            EntityType = DocumentEntityType.Unit,
            EntityId = Guid.NewGuid(),
            FilePathOnNas = "units/1/photo.jpg",
            OriginalFileName = "photo.jpg",
            Category = DocumentCategory.InspectionPhoto,
            UploadedBy = "clerk",
            UploadedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sync = new DocumentVaultMetadataSync(db, NullLogger<DocumentVaultMetadataSync>.Instance);
        await sync.SoftDeleteMatchingAsync("units/1", ["photo.jpg"]);

        (await db.Documents.SingleAsync()).IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task RenameAsync_UpdatesFilePathOnNas()
    {
        using var dbFactory = new SqliteTestDatabase();
        await using var db = dbFactory.CreateContext();
        db.Documents.Add(new Document
        {
            Id = Guid.NewGuid(),
            EntityType = DocumentEntityType.Unit,
            EntityId = Guid.NewGuid(),
            FilePathOnNas = "units/1/old.pdf",
            OriginalFileName = "old.pdf",
            Category = DocumentCategory.Other,
            UploadedBy = "clerk",
            UploadedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sync = new DocumentVaultMetadataSync(db, NullLogger<DocumentVaultMetadataSync>.Instance);
        await sync.RenameAsync("units/1", "old.pdf", "new.pdf");

        var doc = await db.Documents.SingleAsync();
        doc.FilePathOnNas.Should().Be("units/1/new.pdf");
        doc.OriginalFileName.Should().Be("new.pdf");
    }

    [Fact]
    public void CombineRelative_NormalizesPaths()
    {
        DocumentVaultMetadataSync.CombineRelative("/units/1/", "a.pdf").Should().Be("units/1/a.pdf");
        DocumentVaultMetadataSync.CombineRelative("", "root.txt").Should().Be("root.txt");
    }
}
