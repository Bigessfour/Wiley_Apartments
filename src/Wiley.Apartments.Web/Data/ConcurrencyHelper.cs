using Microsoft.EntityFrameworkCore;
using Wiley.Apartments.Domain;

namespace Wiley.Apartments.Web.Data;

public static class ConcurrencyHelper
{
    /// <summary>
    /// Blazor Server shares one DbContext per circuit. Drop tracked rows before a write
    /// so a stale RowVersion from an earlier page does not fail SaveChanges.
    /// </summary>
    public static void DiscardTrackedEntities(DbContext db) => db.ChangeTracker.Clear();

    public static async Task SaveChangesOrThrowAsync(
        DbContext db,
        string entityName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException(entityName);
        }
    }

    public static void BumpRowVersion(Unit unit) => unit.RowVersion = Guid.NewGuid();

    public static void BumpRowVersion(Tenant tenant) => tenant.RowVersion = Guid.NewGuid();

    public static void BumpRowVersion(Lease lease) => lease.RowVersion = Guid.NewGuid();

    public static void BumpRowVersion(FacilityRenter renter) => renter.RowVersion = Guid.NewGuid();

    public static void BumpRowVersion(FacilityReservation reservation) => reservation.RowVersion = Guid.NewGuid();

    public static void BumpRowVersion(FacilityInspection inspection) => inspection.RowVersion = Guid.NewGuid();

    public static void BumpRowVersion(FacilityInventoryItem item) => item.RowVersion = Guid.NewGuid();
}
