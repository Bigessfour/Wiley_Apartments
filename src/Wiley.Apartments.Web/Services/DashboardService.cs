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
            units.Count,
            units.Count(u => u.Status == UnitStatus.Occupied),
            units.Count(u => u.Status == UnitStatus.Vacant),
            units.Count(u => u.Status == UnitStatus.Maintenance),
            units.Count(u => u.Status == UnitStatus.MakeReady),
            expiringWithin30,
            expiringWithin60,
            openWo,
            delinquencies,
            warranties,
            reminders,
            now);
    }

    private static DashboardLeaseRow ToLeaseRow(Lease l, DateTime now) =>
        new(
            l.Id,
            l.Unit?.Number ?? "",
            l.Tenant is null ? "" : $"{l.Tenant.LastName}, {l.Tenant.FirstName}",
            l.EndUtc,
            Math.Max(0, (int)(l.EndUtc.Date - now.Date).TotalDays));
}
