using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Wiley.Apartments.Domain;

namespace Wiley.Apartments.Web.Data;

public static class AuditLogAppender
{
    private static readonly HashSet<string> SensitivePropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "PasswordHash",
        "SecurityStamp",
        "ConcurrencyStamp",
        "NormalizedEmail",
        "NormalizedUserName"
    };

    public static IReadOnlyList<AuditLog> CreateEntries(
        DbContext context,
        string userId,
        DateTime timestampUtc)
    {
        var entries = new List<AuditLog>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is AuditLog || entry.State is EntityState.Detached or EntityState.Unchanged)
            {
                continue;
            }

            entries.Add(MapEntry(entry, userId, timestampUtc));
        }

        return entries;
    }

    public static void Append(DbContext context, string userId, DateTime timestampUtc)
    {
        foreach (var audit in CreateEntries(context, userId, timestampUtc))
        {
            context.Set<AuditLog>().Add(audit);
        }
    }

    internal static AuditLog MapEntry(EntityEntry entry, string userId, DateTime timestampUtc)
    {
        var entityType = entry.Metadata.ClrType.Name;
        var entityId = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString()
            ?? "unknown";
        var action = entry.State switch
        {
            EntityState.Added => "Create",
            EntityState.Modified => "Update",
            EntityState.Deleted => "Delete",
            _ => entry.State.ToString()
        };

        string? oldValues = null;
        string? newValues = null;

        if (entry.State == EntityState.Modified || entry.State == EntityState.Deleted)
        {
            oldValues = JsonSerializer.Serialize(
                entry.Properties
                    .Where(p => !SensitivePropertyNames.Contains(p.Metadata.Name))
                    .ToDictionary(p => p.Metadata.Name, p => p.OriginalValue));
        }

        if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
        {
            newValues = JsonSerializer.Serialize(
                entry.Properties
                    .Where(p => !SensitivePropertyNames.Contains(p.Metadata.Name))
                    .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue));
        }

        return new AuditLog
        {
            UserId = userId,
            TimestampUtc = timestampUtc,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            OldValues = oldValues,
            NewValues = newValues
        };
    }
}
