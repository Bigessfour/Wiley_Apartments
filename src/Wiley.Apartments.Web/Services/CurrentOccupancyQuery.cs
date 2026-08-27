using Microsoft.EntityFrameworkCore;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.Web.Services;

/// <summary>
/// Current operations = occupied unit with an open occupancy (or CurrentTenantId roster fallback).
/// Vacant / make-ready units do not contribute, even if a former CurrentTenantId was left behind.
/// </summary>
internal static class CurrentOccupancyQuery
{
    public static async Task<HashSet<(Guid TenantId, Guid UnitId)>> LoadPairsAsync(
        ApartmentsDbContext db,
        CancellationToken cancellationToken = default)
    {
        var fromOccupancy = await db.Occupancies.AsNoTracking()
            .Where(o => o.EndUtc == null
                && db.Units.Any(u =>
                    u.Id == o.UnitId
                    && !u.IsFacility
                    && u.Status == UnitStatus.Occupied))
            .Select(o => new { o.TenantId, o.UnitId })
            .ToListAsync(cancellationToken);

        var fromRoster = await db.Units.AsNoTracking()
            .Where(u => !u.IsFacility
                && u.Status == UnitStatus.Occupied
                && u.CurrentTenantId != null)
            .Select(u => new { TenantId = u.CurrentTenantId!.Value, UnitId = u.Id })
            .ToListAsync(cancellationToken);

        var set = new HashSet<(Guid TenantId, Guid UnitId)>(fromOccupancy.Count + fromRoster.Count);
        foreach (var row in fromOccupancy)
        {
            set.Add((row.TenantId, row.UnitId));
        }

        foreach (var row in fromRoster)
        {
            set.Add((row.TenantId, row.UnitId));
        }

        return set;
    }
}
