using Microsoft.EntityFrameworkCore;
using Wiley.Apartments.Contracts;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.Web.Services;

public sealed class RentRollService : IRentRollService
{
    private readonly ApartmentsDbContext _db;
    private readonly ILedgerService _ledger;

    public RentRollService(ApartmentsDbContext db, ILedgerService ledger)
    {
        _db = db;
        _ledger = ledger;
    }

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

        var rows = new List<RentRollRow>(units.Count);
        foreach (var unit in units)
        {
            leaseByUnit.TryGetValue(unit.Id, out var lease);
            var tenantId = lease?.TenantId ?? unit.CurrentTenantId;
            decimal balance = 0m;
            if (tenantId is Guid tid)
            {
                balance = await _ledger.GetBalanceAsync(tid, unit.Id, cancellationToken);
            }

            string? tenantName = null;
            if (lease?.Tenant is not null)
            {
                tenantName = $"{lease.Tenant.LastName}, {lease.Tenant.FirstName}";
            }
            else if (tenantId is Guid tid2)
            {
                var t = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(x => x.Id == tid2, cancellationToken);
                if (t is not null)
                {
                    tenantName = $"{t.LastName}, {t.FirstName}";
                }
            }

            rows.Add(new RentRollRow(
                unit.Id,
                unit.Number,
                unit.Status.ToString(),
                tenantId,
                tenantName,
                lease?.Id,
                lease?.Rent,
                balance));
        }

        return rows;
    }

    public async Task<IReadOnlyList<DelinquencyRow>> GetDelinquencyAsync(CancellationToken cancellationToken = default)
    {
        var entries = await _db.LedgerEntries.AsNoTracking()
            .Include(e => e.Tenant)
            .Include(e => e.Unit)
            .Where(e => !e.IsDeleted)
            .OrderBy(e => e.DateUtc)
            .ThenBy(e => e.Id)
            .ToListAsync(cancellationToken);

        var groups = entries.GroupBy(e => new { e.TenantId, e.UnitId });
        var rows = new List<DelinquencyRow>();
        foreach (var g in groups)
        {
            decimal balance = 0;
            DateTime? oldestCharge = null;
            foreach (var e in g)
            {
                balance += e.EntryType == LedgerEntryType.Charge ? e.Amount : -e.Amount;
                if (e.EntryType == LedgerEntryType.Charge && !e.IsLateFee)
                {
                    oldestCharge ??= e.DateUtc;
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
                oldestCharge));
        }

        return rows.OrderByDescending(r => r.Balance).ThenBy(r => r.UnitNumber).ToList();
    }
}
