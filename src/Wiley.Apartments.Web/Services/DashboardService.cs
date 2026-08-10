using Microsoft.EntityFrameworkCore;
using Wiley.Apartments.Contracts;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.Web.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly ApartmentsDbContext _db;
    private readonly IRentRollService _rentRoll;
    private readonly ILeaseService _leases;
    private readonly IMaintenanceService _maintenance;
    private readonly IScheduleService _schedule;
    private readonly IDateTimeService _clock;

    public DashboardService(
        ApartmentsDbContext db,
        IRentRollService rentRoll,
        ILeaseService leases,
        IMaintenanceService maintenance,
        IScheduleService schedule,
        IDateTimeService clock)
    {
        _db = db;
        _rentRoll = rentRoll;
        _leases = leases;
        _maintenance = maintenance;
        _schedule = schedule;
        _clock = clock;
    }

    public async Task<DashboardSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var units = await _db.Units.AsNoTracking()
            .Where(u => !u.IsFacility)
            .ToListAsync(cancellationToken);
        var now = _clock.UtcNow;

        var occupied = units.Count(u => u.Status == UnitStatus.Occupied);
        var vacant = units.Count(u => u.Status == UnitStatus.Vacant);
        var maintenance = units.Count(u => u.Status == UnitStatus.Maintenance);
        var makeReady = units.Count(u => u.Status == UnitStatus.MakeReady);
        var total = units.Count;
        var occupancyPercent = total == 0 ? 0d : Math.Round(100d * occupied / total, 1);

        var statusSlices = new List<DashboardStatusSlice>
        {
            new("Occupied", occupied),
            new("Vacant", vacant),
            new("Maintenance", maintenance),
            new("Make-Ready", makeReady)
        }.Where(s => s.Count > 0).ToList();
        if (statusSlices.Count == 0 && total == 0)
        {
            statusSlices.Add(new DashboardStatusSlice("No units", 1));
        }

        var expiring60 = await _leases.GetExpiringWithinAsync(60, cancellationToken);
        var expiring30 = expiring60
            .Where(l => (l.EndUtc.Date - now.Date).TotalDays <= 30)
            .ToList();
        var expiring31To60 = expiring60
            .Where(l => (l.EndUtc.Date - now.Date).TotalDays > 30)
            .ToList();

        var expiringWithin30 = expiring30
            .Select(l => ToLeaseRow(l, now))
            .ToList();
        var expiringWithin60 = expiring31To60
            .Select(l => ToLeaseRow(l, now))
            .ToList();

        var openWo = (await _maintenance.GetAllAsync(openOnly: true, cancellationToken))
            .Take(12)
            .Select(m => new DashboardMaintenanceRow(
                m.Id,
                m.Unit?.Number ?? "",
                m.Priority.ToString(),
                m.Status.ToString(),
                m.Description))
            .ToList();

        var delinquencies = (await _rentRoll.GetDelinquencyAsync(cancellationToken))
            .Take(12)
            .ToList();
        var outstanding = delinquencies.Sum(d => d.Balance);

        var rentRoll = await _rentRoll.GetRentRollAsync(cancellationToken);
        var expectedRent = rentRoll
            .Where(r => !string.Equals(r.UnitNumber, "CC", StringComparison.OrdinalIgnoreCase)
                        && r.Status == UnitStatus.Occupied.ToString()
                        && r.Rent is > 0)
            .Sum(r => r.Rent!.Value);

        var (monthStartUtc, monthEndUtc) = CurrentLocalMonthUtcRange(now);
        var collectedThisMonth = await _db.LedgerEntries.AsNoTracking()
            .Where(e => !e.IsDeleted
                        && e.EntryType == LedgerEntryType.Payment
                        && !e.IsDeposit
                        && e.DateUtc >= monthStartUtc
                        && e.DateUtc < monthEndUtc)
            .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;

        var collectionByMonth = await BuildCollectionByMonthAsync(now, cancellationToken);
        var heatmap = await BuildPaymentHeatmapAsync(units, now, cancellationToken);
        var collectionRate = expectedRent <= 0m
            ? 0d
            : Math.Round((double)(100m * collectedThisMonth / expectedRent), 1);

        var warrantyCutoff = DateOnly.FromDateTime(now.AddDays(90));
        var today = DateOnly.FromDateTime(now);
        var assets = await _db.Assets.AsNoTracking()
            .Include(a => a.Unit)
            .Where(a => a.WarrantyEnd != null)
            .ToListAsync(cancellationToken);
        var warranties = assets
            .Where(a => a.WarrantyEnd >= today && a.WarrantyEnd <= warrantyCutoff)
            .OrderBy(a => a.WarrantyEnd)
            .Take(12)
            .Select(a => new DashboardWarrantyRow(
                a.Id,
                a.UnitId,
                a.Unit?.Number ?? "",
                $"{a.Type} ({a.Serial})",
                a.WarrantyEnd!.Value,
                a.WarrantyEnd.Value.DayNumber - today.DayNumber))
            .ToList();

        var reminderWindowEnd = now.AddDays(14);
        var scheduleItems = await _schedule.QueryAsync(includeCompleted: false, cancellationToken: cancellationToken);
        var reminders = scheduleItems
            .Select(item =>
            {
                var anchor = item.DueUtc ?? item.StartUtc;
                var reminderUtc = item.ReminderOffset is TimeSpan offset
                    ? anchor - offset
                    : anchor;
                return new { Item = item, ReminderUtc = reminderUtc, Anchor = anchor };
            })
            .Where(x => x.ReminderUtc <= reminderWindowEnd)
            .OrderBy(x => x.ReminderUtc)
            .Take(12)
            .Select(x => new DashboardScheduleReminderRow(
                x.Item.Id,
                x.Item.Title,
                x.Item.Unit?.Number ?? "—",
                x.ReminderUtc,
                x.Anchor,
                x.Item.Category.ToString()))
            .ToList();

        return new DashboardSnapshot(
            total,
            occupied,
            vacant,
            maintenance,
            makeReady,
            expiringWithin30,
            expiringWithin60,
            openWo,
            delinquencies,
            warranties,
            reminders,
            now,
            occupancyPercent,
            expectedRent,
            collectedThisMonth,
            outstanding,
            statusSlices,
            collectionByMonth,
            collectionRate,
            heatmap);
    }

    private (DateTime StartUtc, DateTime EndUtc) CurrentLocalMonthUtcRange(DateTime utcNow)
    {
        var local = _clock.ToDisplayTime(utcNow);
        var startLocal = new DateTime(local.Year, local.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var endLocal = startLocal.AddMonths(1);
        return (_clock.ToUtc(startLocal), _clock.ToUtc(endLocal));
    }

    private async Task<IReadOnlyList<DashboardMonthAmount>> BuildCollectionByMonthAsync(
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var localNow = _clock.ToDisplayTime(utcNow);
        var startLocal = new DateTime(localNow.Year, localNow.Month, 1, 0, 0, 0, DateTimeKind.Unspecified)
            .AddMonths(-11);
        var endLocal = new DateTime(localNow.Year, localNow.Month, 1, 0, 0, 0, DateTimeKind.Unspecified)
            .AddMonths(1);
        var startUtc = _clock.ToUtc(startLocal);
        var endUtc = _clock.ToUtc(endLocal);

        var payments = await _db.LedgerEntries.AsNoTracking()
            .Where(e => !e.IsDeleted
                        && e.EntryType == LedgerEntryType.Payment
                        && !e.IsDeposit
                        && e.DateUtc >= startUtc
                        && e.DateUtc < endUtc)
            .Select(e => new { e.DateUtc, e.Amount })
            .ToListAsync(cancellationToken);

        var byMonth = payments
            .GroupBy(p =>
            {
                var local = _clock.ToDisplayTime(p.DateUtc);
                return new DateTime(local.Year, local.Month, 1);
            })
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        var series = new List<DashboardMonthAmount>(12);
        for (var i = 0; i < 12; i++)
        {
            var month = startLocal.AddMonths(i);
            byMonth.TryGetValue(month, out var amount);
            series.Add(new DashboardMonthAmount(month.ToString("MMM yy"), amount));
        }

        return series;
    }

    private async Task<IReadOnlyList<DashboardHeatCell>> BuildPaymentHeatmapAsync(
        List<Unit> residentialUnits,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var localNow = _clock.ToDisplayTime(utcNow);
        var startLocal = new DateTime(localNow.Year, localNow.Month, 1, 0, 0, 0, DateTimeKind.Unspecified)
            .AddMonths(-11);
        var endLocal = startLocal.AddMonths(12);
        var startUtc = _clock.ToUtc(startLocal);
        var endUtc = _clock.ToUtc(endLocal);

        var unitIds = residentialUnits.Select(u => u.Id).ToList();
        var payments = await _db.LedgerEntries.AsNoTracking()
            .Where(e => !e.IsDeleted
                        && e.EntryType == LedgerEntryType.Payment
                        && !e.IsDeposit
                        && unitIds.Contains(e.UnitId)
                        && e.DateUtc >= startUtc
                        && e.DateUtc < endUtc)
            .Select(e => new { e.UnitId, e.DateUtc, e.Amount })
            .ToListAsync(cancellationToken);

        var keyed = payments
            .GroupBy(p =>
            {
                var local = _clock.ToDisplayTime(p.DateUtc);
                return (p.UnitId, Month: new DateTime(local.Year, local.Month, 1));
            })
            .ToDictionary(g => g.Key, g => (double)g.Sum(x => x.Amount));

        var cells = new List<DashboardHeatCell>(residentialUnits.Count * 12);
        var orderedUnits = residentialUnits.OrderBy(u => u.Number).ToList();
        for (var i = 0; i < 12; i++)
        {
            var month = startLocal.AddMonths(i);
            var monthLabel = month.ToString("MMM yy");
            foreach (var unit in orderedUnits)
            {
                keyed.TryGetValue((unit.Id, month), out var value);
                cells.Add(new DashboardHeatCell(unit.Number, monthLabel, value));
            }
        }

        return cells;
    }

    public async Task<IReadOnlyList<RentPivotRow>> GetRentPivotRowsAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.LedgerEntries.AsNoTracking()
            .Include(e => e.Unit)
            .Where(e => !e.IsDeleted && e.Unit != null && !e.Unit.IsFacility)
            .OrderBy(e => e.DateUtc)
            .Select(e => new
            {
                Unit = e.Unit!.Number,
                e.DateUtc,
                e.Amount,
                e.EntryType,
                e.IsDeposit,
                e.IsLateFee
            })
            .ToListAsync(cancellationToken);

        return rows.Select(e =>
        {
            var local = _clock.ToDisplayTime(e.DateUtc);
            var type = e.EntryType == LedgerEntryType.Charge
                ? (e.IsLateFee ? "Late fee" : e.IsDeposit ? "Deposit charge" : "Rent charge")
                : (e.IsDeposit ? "Deposit payment" : "Rent payment");
            return new RentPivotRow(
                e.Unit,
                local.Year.ToString(),
                local.ToString("MMM"),
                e.EntryType == LedgerEntryType.Charge ? e.Amount : e.Amount,
                type,
                e.EntryType == LedgerEntryType.Payment ? e.Amount : 0m,
                e.EntryType == LedgerEntryType.Charge ? e.Amount : 0m);
        }).ToList();
    }

    private static DashboardLeaseRow ToLeaseRow(Lease l, DateTime now) =>
        new(
            l.Id,
            l.Unit?.Number ?? "",
            l.Tenant is null ? "" : $"{l.Tenant.LastName}, {l.Tenant.FirstName}",
            l.EndUtc,
            Math.Max(0, (int)(l.EndUtc.Date - now.Date).TotalDays));
}
