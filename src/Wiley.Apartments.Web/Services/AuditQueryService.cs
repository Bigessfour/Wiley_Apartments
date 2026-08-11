using Microsoft.EntityFrameworkCore;
using Wiley.Apartments.Contracts;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.Web.Services;

public sealed class AuditQueryService : IAuditQueryService
{
    private readonly ApartmentsDbContext _db;

    public AuditQueryService(ApartmentsDbContext db) => _db = db;

    public async Task<IReadOnlyList<AuditLog>> QueryAsync(
        string? entityType = null,
        string? userId = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        int take = 200,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 500);
        var query = _db.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(a => a.EntityType == entityType);
        }

        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(a => a.UserId.Contains(userId));
        }

        if (fromUtc is not null)
        {
            query = query.Where(a => a.TimestampUtc >= fromUtc);
        }

        if (toUtc is not null)
        {
            query = query.Where(a => a.TimestampUtc <= toUtc);
        }

        return await query
            .OrderByDescending(a => a.TimestampUtc)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
