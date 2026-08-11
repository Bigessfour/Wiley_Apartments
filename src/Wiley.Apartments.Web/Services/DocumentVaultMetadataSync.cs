using Microsoft.EntityFrameworkCore;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.Web.Services;

/// <summary>Keeps <see cref="Document.FilePathOnNas"/> aligned with FileManager delete/rename/move (FR-019).</summary>
public interface IDocumentVaultMetadataSync
{
    Task SoftDeleteMatchingAsync(string folderPath, IEnumerable<string> names, CancellationToken cancellationToken = default);

    Task RenameAsync(
        string folderPath,
        string oldName,
        string newName,
        CancellationToken cancellationToken = default);

    Task MoveAsync(
        string sourceFolderPath,
        string targetFolderPath,
        IEnumerable<string> names,
        CancellationToken cancellationToken = default);
}

public sealed class DocumentVaultMetadataSync : IDocumentVaultMetadataSync
{
    private readonly ApartmentsDbContext _db;
    private readonly ILogger<DocumentVaultMetadataSync> _logger;

    public DocumentVaultMetadataSync(ApartmentsDbContext db, ILogger<DocumentVaultMetadataSync> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SoftDeleteMatchingAsync(
        string folderPath,
        IEnumerable<string> names,
        CancellationToken cancellationToken = default)
    {
        var relativePrefixes = names
            .Select(n => CombineRelative(folderPath, n))
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList();

        if (relativePrefixes.Count == 0)
        {
            return;
        }

        var docs = await _db.Documents
            .Where(d => !d.IsDeleted)
            .ToListAsync(cancellationToken);

        var matched = docs.Where(d => relativePrefixes.Any(p =>
            d.FilePathOnNas.Equals(p, StringComparison.OrdinalIgnoreCase)
            || d.FilePathOnNas.StartsWith(p.TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase))).ToList();

        foreach (var doc in matched)
        {
            doc.IsDeleted = true;
        }

        if (matched.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Soft-deleted {Count} Document metadata rows after vault delete.", matched.Count);
        }
    }

    public async Task RenameAsync(
        string folderPath,
        string oldName,
        string newName,
        CancellationToken cancellationToken = default)
    {
        var oldRel = CombineRelative(folderPath, oldName);
        var newRel = CombineRelative(folderPath, newName);
        if (string.IsNullOrEmpty(oldRel) || string.IsNullOrEmpty(newRel))
        {
            return;
        }

        var docs = await _db.Documents
            .Where(d => !d.IsDeleted && (d.FilePathOnNas == oldRel
                || d.FilePathOnNas.StartsWith(oldRel + "/")))
            .ToListAsync(cancellationToken);

        foreach (var doc in docs)
        {
            if (doc.FilePathOnNas.Equals(oldRel, StringComparison.OrdinalIgnoreCase))
            {
                doc.FilePathOnNas = newRel;
                doc.OriginalFileName = newName;
            }
            else if (doc.FilePathOnNas.StartsWith(oldRel + "/", StringComparison.OrdinalIgnoreCase))
            {
                doc.FilePathOnNas = newRel + doc.FilePathOnNas[oldRel.Length..];
            }
        }

        if (docs.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Renamed {Count} Document metadata rows: {Old} → {New}",
                docs.Count, oldRel, newRel);
        }
    }

    public async Task MoveAsync(
        string sourceFolderPath,
        string targetFolderPath,
        IEnumerable<string> names,
        CancellationToken cancellationToken = default)
    {
        var nameList = names.ToList();
        var updated = 0;
        foreach (var name in nameList)
        {
            var oldRel = CombineRelative(sourceFolderPath, name);
            var newRel = CombineRelative(targetFolderPath, name);
            if (string.IsNullOrEmpty(oldRel) || string.IsNullOrEmpty(newRel))
            {
                continue;
            }

            var docs = await _db.Documents
                .Where(d => !d.IsDeleted && (d.FilePathOnNas == oldRel
                    || d.FilePathOnNas.StartsWith(oldRel + "/")))
                .ToListAsync(cancellationToken);

            foreach (var doc in docs)
            {
                if (doc.FilePathOnNas.Equals(oldRel, StringComparison.OrdinalIgnoreCase))
                {
                    doc.FilePathOnNas = newRel;
                }
                else if (doc.FilePathOnNas.StartsWith(oldRel + "/", StringComparison.OrdinalIgnoreCase))
                {
                    doc.FilePathOnNas = newRel + doc.FilePathOnNas[oldRel.Length..];
                }

                updated++;
            }
        }

        if (updated > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Moved {Count} Document metadata rows from {Source} to {Target}.",
                updated, sourceFolderPath, targetFolderPath);
        }
    }

    /// <summary>Normalize FileManager path + name into Document.FilePathOnNas style (no leading slash).</summary>
    internal static string CombineRelative(string? folderPath, string? name)
    {
        var folder = (folderPath ?? string.Empty).Replace('\\', '/').Trim('/');
        var file = (name ?? string.Empty).Replace('\\', '/').Trim('/');
        if (string.IsNullOrEmpty(file))
        {
            return folder;
        }

        return string.IsNullOrEmpty(folder) ? file : $"{folder}/{file}";
    }
}
