using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Wiley.Apartments.Domain;

namespace Wiley.Apartments.Web.Data;

public sealed class AuditSaveChangesInterceptor(IHttpContextAccessor httpContextAccessor) : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        AppendAuditEntries(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AppendAuditEntries(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AppendAuditEntries(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        EnforceAuditLogAppendOnly(context);

        var userId = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "system";
        AuditLogAppender.Append(context, userId, DateTime.UtcNow);
    }

    /// <summary>Constitution III — AuditLog rows may only be inserted, never updated or deleted.</summary>
    internal static void EnforceAuditLogAppendOnly(DbContext context)
    {
        foreach (var entry in context.ChangeTracker.Entries<AuditLog>())
        {
            if (entry.State is EntityState.Modified or EntityState.Deleted)
            {
                throw new InvalidOperationException(
                    "AuditLog is append-only and cannot be updated or deleted (Constitution III).");
            }
        }
    }
}
