using Microsoft.EntityFrameworkCore;
using Wiley.Apartments.Contracts;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.Web.Services;

public sealed class LedgerService : ILedgerService
{
    private readonly ApartmentsDbContext _db;
    private readonly ILateFeeSettingsService _lateFees;
    private readonly IDateTimeService _clock;
    private readonly ILogger<LedgerService> _logger;

    public LedgerService(
        ApartmentsDbContext db,
        ILateFeeSettingsService lateFees,
        IDateTimeService clock,
        ILogger<LedgerService> logger)
    {
        _db = db;
        _lateFees = lateFees;
        _clock = clock;
        _logger = logger;
    }

    public async Task<LedgerEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.LedgerEntries
            .AsNoTracking()
            .Include(e => e.Tenant)
            .Include(e => e.Unit)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<LedgerLine>> GetLedgerAsync(
        Guid? tenantId = null,
        Guid? unitId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.LedgerEntries
            .AsNoTracking()
            .Include(e => e.Tenant)
            .Include(e => e.Unit)
            .Where(e => !e.IsDeleted);

        if (tenantId is Guid tid)
        {
            query = query.Where(e => e.TenantId == tid);
        }

        if (unitId is Guid uid)
        {
            query = query.Where(e => e.UnitId == uid);
        }

        var entries = await query
            .OrderBy(e => e.DateUtc)
            .ThenBy(e => e.Id)
            .ToListAsync(cancellationToken);

        decimal running = 0;
        var lines = new List<LedgerLine>(entries.Count);
        foreach (var entry in entries)
        {
            running += SignedAmount(entry);
            lines.Add(new LedgerLine(entry, running));
        }

        return lines;
    }

    public async Task<decimal> GetBalanceAsync(
        Guid tenantId,
        Guid? unitId = null,
        CancellationToken cancellationToken = default)
    {
        var lines = await GetLedgerAsync(tenantId, unitId, cancellationToken);
        return lines.Count == 0 ? 0m : lines[^1].RunningBalance;
    }

    public async Task<LedgerEntry> PostChargeAsync(
        Guid tenantId,
        Guid unitId,
        decimal amount,
        DateTime dateUtc,
        Guid? leaseId = null,
        string? notes = null,
        bool isLateFee = false,
        CancellationToken cancellationToken = default)
    {
        await ValidatePartiesAsync(tenantId, unitId, leaseId, cancellationToken);
        var entry = new LedgerEntry
        {
            Id = Guid.NewGuid(),
            EntryType = LedgerEntryType.Charge,
            TenantId = tenantId,
            UnitId = unitId,
            LeaseId = leaseId,
            Amount = RequirePositiveAmount(amount),
            DateUtc = EnsureUtc(dateUtc),
            Notes = TrimNotes(notes),
            IsLateFee = isLateFee,
            IsDeleted = false
        };

        _db.LedgerEntries.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Posted charge {Id} amount {Amount} tenant {TenantId}.", entry.Id, entry.Amount, tenantId);
        return (await GetByIdAsync(entry.Id, cancellationToken))!;
    }

    public async Task<LedgerEntry> PostPaymentAsync(
        Guid tenantId,
        Guid unitId,
        decimal amount,
        DateTime dateUtc,
        PaymentMethod method,
        Guid? leaseId = null,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        await ValidatePartiesAsync(tenantId, unitId, leaseId, cancellationToken);
        var entry = new LedgerEntry
        {
            Id = Guid.NewGuid(),
            EntryType = LedgerEntryType.Payment,
            TenantId = tenantId,
            UnitId = unitId,
            LeaseId = leaseId,
            Amount = RequirePositiveAmount(amount),
            DateUtc = EnsureUtc(dateUtc),
            Method = method,
            Notes = TrimNotes(notes),
            IsLateFee = false,
            IsDeleted = false
        };

        _db.LedgerEntries.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Posted payment {Id} amount {Amount} method {Method} tenant {TenantId}.",
            entry.Id, entry.Amount, method, tenantId);
        return (await GetByIdAsync(entry.Id, cancellationToken))!;
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entry = await _db.LedgerEntries
                        .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken)
                    ?? throw new InvalidOperationException($"Ledger entry {id} was not found.");
        entry.IsDeleted = true;
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Soft-deleted ledger entry {Id}.", id);
    }

    public async Task<int> ApplyLateFeesAsync(
        DateTime? asOfUtc = null,
        CancellationToken cancellationToken = default)
    {
        var settings = await _lateFees.GetAsync(cancellationToken);
        if (!settings.Enabled || settings.Amount <= 0)
        {
            _logger.LogInformation("Late fee assessment skipped (disabled or zero amount).");
            return 0;
        }

        var asOf = EnsureUtc(asOfUtc ?? _clock.UtcNow);
        var entries = await _db.LedgerEntries
            .AsNoTracking()
            .Where(e => !e.IsDeleted)
            .OrderBy(e => e.DateUtc)
            .ThenBy(e => e.Id)
            .ToListAsync(cancellationToken);

        var assessed = 0;
        foreach (var group in entries.GroupBy(e => new { e.TenantId, e.UnitId }))
        {
            decimal balance = 0;
            var hasPastDueCharge = false;
            var hasLateFeeThisMonth = false;
            foreach (var e in group)
            {
                balance += e.EntryType == LedgerEntryType.Charge ? e.Amount : -e.Amount;
                // Invoice-style: each charge's due window is DateUtc + GraceDays.
                if (e.EntryType == LedgerEntryType.Charge
                    && !e.IsLateFee
                    && e.DateUtc.AddDays(settings.GraceDays) < asOf)
                {
                    hasPastDueCharge = true;
                }

                if (e.IsLateFee
                    && e.DateUtc.Year == asOf.Year
                    && e.DateUtc.Month == asOf.Month)
                {
                    hasLateFeeThisMonth = true;
                }
            }

            if (balance <= 0 || hasLateFeeThisMonth || !hasPastDueCharge)
            {
                continue;
            }

            await PostChargeAsync(
                group.Key.TenantId,
                group.Key.UnitId,
                settings.Amount,
                asOf,
                notes: $"Late fee (grace {settings.GraceDays} days)",
                isLateFee: true,
                cancellationToken: cancellationToken);
            assessed++;
        }

        _logger.LogInformation("Assessed {Count} late fee charge(s) as of {AsOf}.", assessed, asOf);
        return assessed;
    }

    public async Task<int> PostMonthlyRentChargesAsync(
        DateTime? asOfUtc = null,
        CancellationToken cancellationToken = default)
    {
        var asOf = EnsureUtc(asOfUtc ?? _clock.UtcNow);
        var monthStart = new DateTime(asOf.Year, asOf.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);

        var activeLeases = await _db.Leases.AsNoTracking()
            .Where(l => !l.IsDeleted && l.Status == LeaseStatus.Active)
            .ToListAsync(cancellationToken);

        var existingRentCharges = await _db.LedgerEntries.AsNoTracking()
            .Where(e => !e.IsDeleted
                && e.EntryType == LedgerEntryType.Charge
                && !e.IsLateFee
                && e.DateUtc >= monthStart
                && e.DateUtc < monthEnd)
            .Select(e => new { e.TenantId, e.UnitId })
            .ToListAsync(cancellationToken);

        var charged = new HashSet<(Guid TenantId, Guid UnitId)>(
            existingRentCharges.Select(e => (e.TenantId, e.UnitId)));

        var posted = 0;
        foreach (var lease in activeLeases)
        {
            if (charged.Contains((lease.TenantId, lease.UnitId)))
            {
                continue;
            }

            await PostChargeAsync(
                lease.TenantId,
                lease.UnitId,
                lease.Rent,
                monthStart,
                lease.Id,
                notes: $"Rent {monthStart:MMMM yyyy}",
                cancellationToken: cancellationToken);
            charged.Add((lease.TenantId, lease.UnitId));
            posted++;
        }

        // Occupied units with listed MonthlyRent but no active lease (paper leases / import).
        var rosterUnits = await _db.Units.AsNoTracking()
            .Where(u => !u.IsFacility
                && u.CurrentTenantId != null
                && u.MonthlyRent > 0
                && u.Status == UnitStatus.Occupied)
            .ToListAsync(cancellationToken);

        foreach (var unit in rosterUnits)
        {
            var tenantId = unit.CurrentTenantId!.Value;
            if (charged.Contains((tenantId, unit.Id)))
            {
                continue;
            }

            if (activeLeases.Any(l => l.UnitId == unit.Id && l.TenantId == tenantId))
            {
                continue;
            }

            await PostChargeAsync(
                tenantId,
                unit.Id,
                unit.MonthlyRent,
                monthStart,
                leaseId: null,
                notes: $"Rent {monthStart:MMMM yyyy} (unit roster)",
                cancellationToken: cancellationToken);
            charged.Add((tenantId, unit.Id));
            posted++;
        }

        _logger.LogInformation("Posted {Count} monthly rent charge(s) for {Month}.", posted, monthStart.ToString("yyyy-MM"));
        return posted;
    }

    private async Task ValidatePartiesAsync(
        Guid tenantId,
        Guid unitId,
        Guid? leaseId,
        CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants.AsNoTracking()
                         .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken)
                     ?? throw new InvalidOperationException($"Tenant {tenantId} was not found.");
        if (tenant.IsDeleted)
        {
            throw new InvalidOperationException("Cannot post ledger entries for a soft-deleted tenant.");
        }

        if (!await _db.Units.AnyAsync(u => u.Id == unitId, cancellationToken))
        {
            throw new InvalidOperationException($"Unit {unitId} was not found.");
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

            if (lease.TenantId != tenantId || lease.UnitId != unitId)
            {
                throw new InvalidOperationException("Lease does not match tenant/unit.");
            }
        }
    }

    private static decimal SignedAmount(LedgerEntry entry) =>
        entry.EntryType == LedgerEntryType.Charge ? entry.Amount : -entry.Amount;

    private static decimal RequirePositiveAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than zero.", nameof(amount));
        }

        return decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
    }

    private static string? TrimNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return null;
        }

        var trimmed = notes.Trim();
        return trimmed.Length > 2000 ? trimmed[..2000] : trimmed;
    }

    private static DateTime EnsureUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
}
