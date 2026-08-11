using System.Security.Claims;
using System.Text.Json;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.Web.Services;

/// <summary>Append-only audit for NAS FileManager mutations that bypass EF change tracking (Constitution III).</summary>
public interface IDocumentVaultAuditService
{
    Task LogAsync(
        string action,
        string? path,
        IEnumerable<string>? names,
        string? targetPath = null,
        string? newName = null,
        CancellationToken cancellationToken = default);
}

public sealed class DocumentVaultAuditService(
    ApartmentsDbContext db,
    IHttpContextAccessor httpContextAccessor,
    ILogger<DocumentVaultAuditService> logger) : IDocumentVaultAuditService
{
    private readonly ApartmentsDbContext _db = db;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly ILogger<DocumentVaultAuditService> _logger = logger;

    public async Task LogAsync(
        string action,
        string? path,
        IEnumerable<string>? names,
        string? targetPath = null,
        string? newName = null,
        CancellationToken cancellationToken = default)
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? _httpContextAccessor.HttpContext?.User?.Identity?.Name
            ?? "unknown";

        var nameList = names?.Where(n => !string.IsNullOrWhiteSpace(n)).ToList() ?? [];
        var entityId = nameList.Count > 0
            ? string.Join("|", nameList.Take(3))
            : (path ?? "/");

        var before = JsonSerializer.Serialize(new
        {
            path,
            names = nameList
        });
        var after = JsonSerializer.Serialize(new
        {
            path,
            names = nameList,
            targetPath,
            newName
        });

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            TimestampUtc = DateTime.UtcNow,
            EntityType = "DocumentVaultFile",
            EntityId = entityId.Length > 64 ? entityId[..64] : entityId,
            Action = action,
            OldValues = before,
            NewValues = after
        });

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Document vault {Action} by {User}: path={Path} names={Names}",
            action, userId, path, string.Join(",", nameList));
    }
}
