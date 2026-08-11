using Microsoft.EntityFrameworkCore;
using Wiley.Apartments.Contracts;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.Web.Services;

public sealed class DocumentService(
    ApartmentsDbContext db,
    IDocumentPathResolver paths,
    IDateTimeService clock,
    ILogger<DocumentService> logger) : IDocumentService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".docx", ".doc", ".png", ".jpg", ".jpeg", ".webp"
    };

    private const long MaxUploadBytes = 25 * 1024 * 1024;

    private readonly ApartmentsDbContext _db = db;
    private readonly IDocumentPathResolver _paths = paths;
    private readonly IDateTimeService _clock = clock;
    private readonly ILogger<DocumentService> _logger = logger;

    public async Task<IReadOnlyList<DocumentInfo>> ListForEntityAsync(
        DocumentEntityType entityType,
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.Documents
            .AsNoTracking()
            .Where(d => !d.IsDeleted && d.EntityType == entityType && d.EntityId == entityId)
            .OrderByDescending(d => d.UploadedAtUtc)
            .ToListAsync(cancellationToken);
        return [.. rows.Select(Map)];
    }

    public async Task<IReadOnlyList<DocumentInfo>> QueryAsync(
        DocumentEntityType? entityType = null,
        DocumentCategory? category = null,
        int take = 200,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 500);
        var query = _db.Documents.AsNoTracking().Where(d => !d.IsDeleted);
        if (entityType is DocumentEntityType et)
        {
            query = query.Where(d => d.EntityType == et);
        }

        if (category is DocumentCategory cat)
        {
            query = query.Where(d => d.Category == cat);
        }

        var rows = await query
            .OrderByDescending(d => d.UploadedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);
        return [.. rows.Select(Map)];
    }

    public async Task<DocumentInfo?> GetByIdAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.Documents.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted, cancellationToken);
        return row is null ? null : Map(row);
    }

    public async Task<string?> ResolveAbsolutePathAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var doc = await _db.Documents.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted, cancellationToken);
        if (doc is null)
        {
            return null;
        }

        var abs = Path.Combine(
            ResolveDocumentRoot(),
            doc.FilePathOnNas.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(abs) ? abs : null;
    }

    public async Task<DocumentInfo> UploadAsync(
        DocumentEntityType entityType,
        Guid entityId,
        DocumentCategory category,
        string originalFileName,
        string contentType,
        Stream content,
        string uploadedBy,
        string relativeDirectory,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            throw new ArgumentException("File name is required.", nameof(originalFileName));
        }

        var safeName = Path.GetFileName(originalFileName);
        var ext = Path.GetExtension(safeName);
        if (!AllowedExtensions.Contains(ext))
        {
            throw new InvalidOperationException(
                $"File type '{ext}' is not allowed. Use PDF, Word, or common images.");
        }

        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length == 0)
        {
            throw new InvalidOperationException("Uploaded file is empty.");
        }

        if (buffer.Length > MaxUploadBytes)
        {
            throw new InvalidOperationException($"File exceeds {MaxUploadBytes / (1024 * 1024)} MB limit.");
        }

        var root = ResolveDocumentRoot();
        DocumentRootAvailability.EnsureWritable(root);
        var relativeDir = relativeDirectory.Replace('\\', '/').Trim('/');
        var absDir = Path.Combine(root, relativeDir.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(absDir);

        var stamp = _clock.UtcNow.ToString("yyyyMMddHHmmss");
        var storedName = $"{Path.GetFileNameWithoutExtension(safeName)}-{stamp}{ext}";
        storedName = SanitizeFileName(storedName);
        var relativePath = $"{relativeDir}/{storedName}";
        var absPath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));

        buffer.Position = 0;
        await using (var file = File.Create(absPath))
        {
            await buffer.CopyToAsync(file, cancellationToken);
        }

        var doc = new Document
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            FilePathOnNas = relativePath,
            OriginalFileName = safeName,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? GuessContentType(ext) : contentType,
            Category = category,
            UploadedBy = string.IsNullOrWhiteSpace(uploadedBy) ? "system" : uploadedBy,
            UploadedAtUtc = _clock.UtcNow,
            IsDeleted = false
        };

        _db.Documents.Add(doc);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Uploaded document {DocumentId} for {EntityType}/{EntityId} → {Path}",
            doc.Id, entityType, entityId, relativePath);
        return Map(doc);
    }

    public async Task<byte[]?> ReadBytesAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var doc = await _db.Documents.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted, cancellationToken);
        if (doc is null)
        {
            return null;
        }

        var abs = Path.Combine(
            ResolveDocumentRoot(),
            doc.FilePathOnNas.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(abs))
        {
            return null;
        }

        return await File.ReadAllBytesAsync(abs, cancellationToken);
    }

    public async Task SoftDeleteAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var doc = await _db.Documents.FindAsync([documentId], cancellationToken)
            ?? throw new InvalidOperationException($"Document {documentId} was not found.");
        doc.IsDeleted = true;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private string ResolveDocumentRoot() => _paths.GetDocumentRoot();


    private static DocumentInfo Map(Document d) =>
        new(d.Id, d.EntityType, d.EntityId, d.FilePathOnNas, d.OriginalFileName, d.ContentType,
            d.Category, d.UploadedBy, d.UploadedAtUtc);

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }

        return name.Replace(' ', '_');
    }

    private static string GuessContentType(string ext) =>
        ext.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".doc" => "application/msword",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
}
