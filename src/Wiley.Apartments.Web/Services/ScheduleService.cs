using Microsoft.EntityFrameworkCore;
using Wiley.Apartments.Contracts;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.Web.Services;

public sealed class ScheduleService : IScheduleService
{
    private readonly ApartmentsDbContext _db;
    private readonly IDateTimeService _clock;
    private readonly ILogger<ScheduleService> _logger;

    public ScheduleService(
        ApartmentsDbContext db,
        IDateTimeService clock,
        ILogger<ScheduleService> logger)
    {
        _db = db;
        _clock = clock;
        _logger = logger;
    }

    public async Task<ScheduledItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.ScheduledItems
            .AsNoTracking()
            .Include(s => s.Unit)
            .Include(s => s.Tenant)
            .Include(s => s.Lease)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<ScheduledItem>> QueryAsync(
        Guid? unitId = null,
        ScheduledItemCategory? category = null,
        DateTime? rangeStartUtc = null,
        DateTime? rangeEndUtc = null,
        bool includeCompleted = true,
        CancellationToken cancellationToken = default)
    {
        var query = _db.ScheduledItems
            .AsNoTracking()
            .Include(s => s.Unit)
            .Include(s => s.Tenant)
            .Where(s => !s.IsDeleted);

        if (unitId is Guid uid)
        {
            query = query.Where(s => s.UnitId == uid);
        }

        if (category is ScheduledItemCategory cat)
        {
            query = query.Where(s => s.Category == cat);
        }

        if (!includeCompleted)
        {
            query = query.Where(s => !s.IsCompleted);
        }

        if (rangeStartUtc is DateTime rs)
        {
            var start = EnsureUtc(rs);
            query = query.Where(s =>
                (s.EndUtc ?? s.DueUtc ?? s.StartUtc) >= start);
        }

        if (rangeEndUtc is DateTime re)
        {
            var end = EnsureUtc(re);
            query = query.Where(s => s.StartUtc <= end);
        }

        return await query
            .OrderBy(s => s.StartUtc)
            .ThenBy(s => s.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task<ScheduledItem> CreateAsync(
        string title,
        ScheduledItemCategory category,
        DateTime startUtc,
        DateTime? endUtc = null,
        DateTime? dueUtc = null,
        TimeSpan? reminderOffset = null,
        Guid? unitId = null,
        Guid? tenantId = null,
        Guid? leaseId = null,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        var start = EnsureUtc(startUtc);
        var end = endUtc is null ? null : (DateTime?)EnsureUtc(endUtc.Value);
        var due = dueUtc is null ? null : (DateTime?)EnsureUtc(dueUtc.Value);
        ValidateWindow(start, end, due);
        await ValidateLinksAsync(unitId, tenantId, leaseId, cancellationToken);

        var item = new ScheduledItem
        {
            Id = Guid.NewGuid(),
            Title = RequireTitle(title),
            Category = category,
            UnitId = unitId,
            TenantId = tenantId,
            LeaseId = leaseId,
            StartUtc = start,
            EndUtc = end,
            DueUtc = due,
            ReminderOffset = reminderOffset,
            Notes = TrimNotes(notes),
            IsCompleted = false,
            IsDeleted = false
        };

        _db.ScheduledItems.Add(item);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Created scheduled item {Id} ({Category}).", item.Id, item.Category);
        return (await GetByIdAsync(item.Id, cancellationToken))!;
    }

    public async Task<ScheduledItem> UpdateAsync(
        Guid id,
        string title,
        ScheduledItemCategory category,
        DateTime startUtc,
        DateTime? endUtc = null,
        DateTime? dueUtc = null,
        TimeSpan? reminderOffset = null,
        Guid? unitId = null,
        Guid? tenantId = null,
        Guid? leaseId = null,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        var item = await RequireAsync(id, cancellationToken);
        var start = EnsureUtc(startUtc);
        var end = endUtc is null ? null : (DateTime?)EnsureUtc(endUtc.Value);
        var due = dueUtc is null ? null : (DateTime?)EnsureUtc(dueUtc.Value);
        ValidateWindow(start, end, due);
        await ValidateLinksAsync(unitId, tenantId, leaseId, cancellationToken);

        item.Title = RequireTitle(title);
        item.Category = category;
        item.UnitId = unitId;
        item.TenantId = tenantId;
        item.LeaseId = leaseId;
        item.StartUtc = start;
        item.EndUtc = end;
        item.DueUtc = due;
        item.ReminderOffset = reminderOffset;
        item.Notes = TrimNotes(notes);

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Updated scheduled item {Id}.", item.Id);
        return (await GetByIdAsync(item.Id, cancellationToken))!;
    }

    public async Task<ScheduledItem> CompleteAsync(
        Guid id,
        DateTime? completedUtc = null,
        CancellationToken cancellationToken = default)
    {
        var item = await RequireAsync(id, cancellationToken);
        if (item.IsCompleted)
        {
            return (await GetByIdAsync(item.Id, cancellationToken))!;
        }

        item.IsCompleted = true;
        item.CompletedUtc = EnsureUtc(completedUtc ?? _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Completed scheduled item {Id}.", item.Id);
        return (await GetByIdAsync(item.Id, cancellationToken))!;
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await RequireAsync(id, cancellationToken);
        item.IsDeleted = true;
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Soft-deleted scheduled item {Id}.", item.Id);
    }

    private async Task<ScheduledItem> RequireAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _db.ScheduledItems
                   .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken)
               ?? throw new InvalidOperationException($"Scheduled item {id} was not found.");
    }

    private async Task ValidateLinksAsync(
        Guid? unitId,
        Guid? tenantId,
        Guid? leaseId,
        CancellationToken cancellationToken)
    {
        if (unitId is Guid uid &&
            !await _db.Units.AnyAsync(u => u.Id == uid, cancellationToken))
        {
            throw new InvalidOperationException($"Unit {uid} was not found.");
        }

        if (tenantId is Guid tid)
        {
            var tenant = await _db.Tenants.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tid, cancellationToken)
                ?? throw new InvalidOperationException($"Tenant {tid} was not found.");
            if (tenant.IsDeleted)
            {
                throw new InvalidOperationException("Cannot link a soft-deleted tenant.");
            }
        }

        if (leaseId is Guid lid)
        {
            var lease = await _db.Leases.AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == lid, cancellationToken)
                ?? throw new InvalidOperationException($"Lease {lid} was not found.");
            if (lease.IsDeleted)
            {
                throw new InvalidOperationException("Cannot link a soft-deleted lease.");
            }
        }
    }

    private static void ValidateWindow(DateTime startUtc, DateTime? endUtc, DateTime? dueUtc)
    {
        if (endUtc is DateTime end && end < startUtc)
        {
            throw new ArgumentException("End must be on or after start.");
        }

        if (dueUtc is DateTime due && due < startUtc.AddYears(-1))
        {
            // Allow due slightly before start for reminders; reject absurd backdates.
            throw new ArgumentException("Due date is unreasonably before start.");
        }
    }

    private static string RequireTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        var trimmed = title.Trim();
        if (trimmed.Length > 256)
        {
            throw new ArgumentException("Title cannot exceed 256 characters.", nameof(title));
        }

        return trimmed;
    }

    private static string? TrimNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return null;
        }

        var trimmed = notes.Trim();
        if (trimmed.Length > 2000)
        {
            throw new ArgumentException("Notes cannot exceed 2000 characters.");
        }

        return trimmed;
    }

    private static DateTime EnsureUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
}
