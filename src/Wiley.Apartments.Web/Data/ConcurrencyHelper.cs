using Microsoft.EntityFrameworkCore;
using Wiley.Apartments.Domain;

namespace Wiley.Apartments.Web.Data;

public static class ConcurrencyHelper
{
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
}
