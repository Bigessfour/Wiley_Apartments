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
    private readonly IDateTimeService _clock;

    public DashboardService(
        ApartmentsDbContext db,
        IRentRollService rentRoll,
        ILeaseService leases,
        IMaintenanceService maintenance,
        IDateTimeService clock)
    {
        _db = db;
        _rentRoll = rentRoll;
        _leases = leases;
        _maintenance = maintenance;
        _clock = clock;
    }

    public async Task<DashboardSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var units = await _db.Units.AsNoTracking().ToListAsync(cancellationToken);
        var now = _clock.UtcNow;

        var expiring = (await _leases.GetExpiringWithinAsync(60, cancellationToken))
            .Select(l => new DashboardLeaseRow(
                l.Id,
                l.Unit?.Number ?? "",
                l.Tenant is null ? "" : $"{l.Tenant.LastName}, {l.Tenant.FirstName}",
                l.EndUtc,
                Math.Max(0, (int)(l.EndUtc.Date - now.Date).TotalDays)))
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

        return new DashboardSnapshot(
            units.Count,
            units.Count(u => u.Status == UnitStatus.Occupied),
            units.Count(u => u.Status == UnitStatus.Vacant),
            units.Count(u => u.Status == UnitStatus.Maintenance),
            units.Count(u => u.Status == UnitStatus.MakeReady),
            expiring,
            openWo,
            delinquencies,
            warranties,
            now);
    }
}
