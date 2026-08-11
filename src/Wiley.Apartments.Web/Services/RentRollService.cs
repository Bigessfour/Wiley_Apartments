using Microsoft.EntityFrameworkCore;
using Wiley.Apartments.Contracts;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.Web.Services;

public sealed class RentRollService(ApartmentsDbContext db, ILogger<RentRollService> logger) : IRentRollService
{
    private readonly ApartmentsDbContext _db = db;
    private readonly ILogger<RentRollService> _logger = logger;

    public async Task<IReadOnlyList<RentRollRow>> GetRentRollAsync(CancellationToken cancellationToken = default)
    {
        var units = await _db.Units.AsNoTracking()
            .OrderBy(u => u.Number)
            .ToListAsync(cancellationToken);

        var activeLeases = await _db.Leases.AsNoTracking()
            .Include(l => l.Tenant)
            .Where(l => !l.IsDeleted
                && (l.Status == LeaseStatus.Active || l.Status == LeaseStatus.Amended))
            .ToListAsync(cancellationToken);

        var leaseByUnit = activeLeases
            .GroupBy(l => l.UnitId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(l => l.StartUtc).First());

        // One pass over ledger for all unit balances (avoids N GetBalanceAsync calls).
        var balances = await LoadBalancesAsync(cancellationToken);

        var orphanTenantIds = units
            .Select(u =>
            {
                leaseByUnit.TryGetValue(u.Id, out var lease);
                return lease?.TenantId ?? u.CurrentTenantId;
            })
            .Where(id => id is not null)
            .Cast<Guid>()
            .Distinct()
            .Where(id => !activeLeases.Any(l => l.TenantId == id && l.Tenant is not null))
            .ToList();

        var orphanTenants = orphanTenantIds.Count == 0
            ? []
            : await _db.Tenants.AsNoTracking()
                .Where(t => orphanTenantIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, cancellationToken);

        List<RentRollRow> rows = new(units.Count);
        var rentFromLease = 0;
        var rentFromRoster = 0;
        var rentMissing = 0;
        foreach (var unit in units)
        {
            leaseByUnit.TryGetValue(unit.Id, out var lease);
            var tenantId = lease?.TenantId ?? unit.CurrentTenantId;
            decimal balance = 0m;
            if (tenantId is Guid tid
                && balances.TryGetValue((tid, unit.Id), out var bal))
            {
                balance = bal;
            }

            string? tenantName = null;
            if (lease?.Tenant is not null)
            {
                tenantName = $"{lease.Tenant.LastName}, {lease.Tenant.FirstName}";
            }
            else if (tenantId is Guid tid2 && orphanTenants.TryGetValue(tid2, out var t))
            {
                tenantName = $"{t.LastName}, {t.FirstName}";
            }

            decimal? rent;
            if (lease is not null)
            {
                rent = lease.Rent;
                rentFromLease++;
            }
            else if (unit.MonthlyRent > 0)
            {
                rent = unit.MonthlyRent;
                rentFromRoster++;
            }
            else
            {
                rent = null;
                rentMissing++;
            }

            rows.Add(new RentRollRow(
                unit.Id,
                unit.Number,
                unit.Status.ToString(),
                tenantId,
                tenantName,
                lease?.Id,
                rent,
                balance));
        }

        _logger.LogInformation(
            "Rent roll generated: {UnitCount} unit(s), rentFromLease={FromLease}, rentFromRoster={FromRoster}, rentMissing={Missing}.",
            rows.Count,
            rentFromLease,
            rentFromRoster,
            rentMissing);
        return rows;
    }

    public async Task<IReadOnlyList<DelinquencyRow>> GetDelinquencyAsync(CancellationToken cancellationToken = default)
    {
        var entries = await _db.LedgerEntries.AsNoTracking()
            .Include(e => e.Tenant)
            .Include(e => e.Unit)
            .Where(e => !e.IsDeleted && e.TenantId != null)
            .OrderBy(e => e.DateUtc)
            .ThenBy(e => e.Id)
            .ToListAsync(cancellationToken);

        var groups = entries.GroupBy(e => new { TenantId = e.TenantId!.Value, e.UnitId });
        var rows = new List<DelinquencyRow>();
        foreach (var g in groups)
        {
            decimal balance = 0;
            DateTime? oldestPastDueCharge = null;
            foreach (var e in g)
            {
                balance += e.EntryType == LedgerEntryType.Charge ? e.Amount : -e.Amount;
                if (e.EntryType == LedgerEntryType.Charge && !e.IsLateFee)
                {
                    oldestPastDueCharge ??= e.DateUtc;
                }
            }

            if (balance <= 0)
            {
                continue;
            }

            var sample = g.First();
            var name = sample.Tenant is null
                ? ""
                : $"{sample.Tenant.LastName}, {sample.Tenant.FirstName}";
            rows.Add(new DelinquencyRow(
                g.Key.UnitId,
                sample.Unit?.Number ?? "",
                g.Key.TenantId,
                name,
                balance,
                oldestPastDueCharge));
        }

        var result = rows.OrderByDescending(r => r.Balance).ThenBy(r => r.UnitNumber).ToList();
        _logger.LogInformation(
            "Delinquency report generated: {DelinquentCount} account(s) with positive balance.",
            result.Count);
        return result;
    }

    private async Task<Dictionary<(Guid TenantId, Guid UnitId), decimal>> LoadBalancesAsync(
        CancellationToken cancellationToken)
    {
        var entries = await _db.LedgerEntries.AsNoTracking()
            .Where(e => !e.IsDeleted && e.TenantId != null)
            .Select(e => new { TenantId = e.TenantId!.Value, e.UnitId, e.EntryType, e.Amount })
            .ToListAsync(cancellationToken);

        var map = new Dictionary<(Guid, Guid), decimal>();
        foreach (var e in entries)
        {
            var key = (e.TenantId, e.UnitId);
            map.TryGetValue(key, out var bal);
            bal += e.EntryType == LedgerEntryType.Charge ? e.Amount : -e.Amount;
            map[key] = bal;
        }

        return map;
    }
}
