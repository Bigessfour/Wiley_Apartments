using Microsoft.EntityFrameworkCore;
using Wiley.Apartments.Contracts;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.Web.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly ApartmentsDbContext _db;
    private readonly IDateTimeService _clock;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        ApartmentsDbContext db,
        IDateTimeService clock,
        ILogger<DashboardService> logger)
    {
        _db = db;
        _clock = clock;
        _logger = logger;
    }

    public async Task<DashboardSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var units = await _db.Units
            .AsNoTracking()
            .OrderBy(u => u.Number)
            .ToListAsync(cancellationToken);

        var total = units.Count;
        var occupied = units.Count(u => u.Status == UnitStatus.Occupied);
        var vacant = units.Count(u => u.Status == UnitStatus.Vacant);
        var makeReady = units.Count(u => u.Status == UnitStatus.MakeReady);
        var maintenance = units.Count(u => u.Status == UnitStatus.Maintenance);
        var occupancyRate = total == 0 ? 0 : Math.Round(occupied * 1000.0 / total) / 10.0;

        var today = DateOnly.FromDateTime(_clock.ToDisplayTime(_clock.UtcNow));
        var horizon = today.AddDays(90);

        var assets = await _db.Assets
            .AsNoTracking()
            .Include(a => a.Unit)
            .Where(a => a.WarrantyEnd != null && a.WarrantyEnd >= today && a.WarrantyEnd <= horizon)
            .OrderBy(a => a.WarrantyEnd)
            .Take(25)
            .ToListAsync(cancellationToken);

        var warrantyAlerts = assets
            .Where(a => a.WarrantyEnd.HasValue && a.Unit is not null)
            .Select(a => new WarrantyAlertItem
            {
                AssetId = a.Id,
                UnitId = a.UnitId,
                UnitNumber = a.Unit!.Number,
                AssetType = a.Type,
                WarrantyEnd = a.WarrantyEnd!.Value,
                DaysLeft = a.WarrantyEnd.Value.DayNumber - today.DayNumber,
            })
            .ToList();

        var breakdown = new List<StatusBreakdownItem>
        {
            new() { Name = "Occupied", Count = occupied },
            new() { Name = "Vacant", Count = vacant },
            new() { Name = "Make-Ready", Count = makeReady },
            new() { Name = "Maintenance", Count = maintenance },
        }.Where(b => b.Count > 0).ToList();

        var rows = units.Select(u => new UnitStatusRow
        {
            Id = u.Id,
            Number = u.Number,
            Status = FormatStatus(u.Status),
            Beds = u.Beds,
            Baths = u.Baths,
            SqFt = u.SqFt,
        }).ToList();

        _logger.LogDebug(
            "Dashboard snapshot: {Total} units, occupancy {Rate}%, {Warranty} warranty alerts.",
            total, occupancyRate, warrantyAlerts.Count);

        return new DashboardSnapshot
        {
            TotalUnits = total,
            OccupiedCount = occupied,
            VacantCount = vacant,
            MakeReadyCount = makeReady,
            MaintenanceCount = maintenance,
            OccupancyRate = occupancyRate,
            // Deferred until payments / maintenance modules (G2 / maintenance domain).
            OpenWorkOrders = 0,
            DelinquentCount = 0,
            DelinquentAmount = 0m,
            WarrantyAlerts = warrantyAlerts,
            Units = rows,
            StatusBreakdown = breakdown,
        };
    }

    private static string FormatStatus(UnitStatus status) => status switch
    {
        UnitStatus.Occupied => "Occupied",
        UnitStatus.Vacant => "Vacant",
        UnitStatus.Maintenance => "Maintenance",
        UnitStatus.MakeReady => "Make-Ready",
        _ => status.ToString(),
    };
}
