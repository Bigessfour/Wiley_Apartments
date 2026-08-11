using Microsoft.Extensions.Logging.Abstractions;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Services;
using Wiley.Apartments.Tests.Support;

namespace Wiley.Apartments.Tests.Services;

public class DashboardServiceTests
{
    [Fact]
    public async Task GetSnapshotAsync_ComputesOccupancyAndStatusCounts()
    {
        using var fixture = new SqliteTestDatabase();
        await using var db = fixture.CreateContext();
        db.Units.AddRange(
            new Unit { Id = Guid.NewGuid(), Number = "1", Status = UnitStatus.Occupied, Beds = 2, Baths = 1, SqFt = 800 },
            new Unit { Id = Guid.NewGuid(), Number = "2", Status = UnitStatus.Vacant, Beds = 1, Baths = 1, SqFt = 600 },
            new Unit { Id = Guid.NewGuid(), Number = "3", Status = UnitStatus.MakeReady, Beds = 2, Baths = 1, SqFt = 780 },
            new Unit { Id = Guid.NewGuid(), Number = "4", Status = UnitStatus.Maintenance, Beds = 2, Baths = 1, SqFt = 790 });
        await db.SaveChangesAsync();

        var clock = new FixedClock(new DateTime(2026, 8, 11, 15, 0, 0, DateTimeKind.Utc));
        var sut = new DashboardService(db, clock, NullLogger<DashboardService>.Instance);

        var snap = await sut.GetSnapshotAsync();

        Assert.Equal(4, snap.TotalUnits);
        Assert.Equal(1, snap.OccupiedCount);
        Assert.Equal(1, snap.VacantCount);
        Assert.Equal(1, snap.MakeReadyCount);
        Assert.Equal(1, snap.MaintenanceCount);
        Assert.Equal(25.0, snap.OccupancyRate);
        Assert.Equal(0, snap.OpenWorkOrders);
        Assert.Equal(0, snap.DelinquentCount);
        Assert.Equal(4, snap.Units.Count);
    }

    [Fact]
    public async Task GetSnapshotAsync_IncludesWarrantiesWithin90Days()
    {
        using var fixture = new SqliteTestDatabase();
        await using var db = fixture.CreateContext();
        var unitId = Guid.NewGuid();
        db.Units.Add(new Unit { Id = unitId, Number = "10", Status = UnitStatus.Occupied, Beds = 2, Baths = 1, SqFt = 800 });
        db.Assets.AddRange(
            new Asset
            {
                Id = Guid.NewGuid(),
                UnitId = unitId,
                Type = "Water heater",
                Make = "Rheem",
                WarrantyEnd = new DateOnly(2026, 9, 1),
            },
            new Asset
            {
                Id = Guid.NewGuid(),
                UnitId = unitId,
                Type = "Old fridge",
                Make = "GE",
                WarrantyEnd = new DateOnly(2027, 12, 1),
            });
        await db.SaveChangesAsync();

        var clock = new FixedClock(new DateTime(2026, 8, 11, 15, 0, 0, DateTimeKind.Utc));
        var sut = new DashboardService(db, clock, NullLogger<DashboardService>.Instance);

        var snap = await sut.GetSnapshotAsync();

        Assert.Single(snap.WarrantyAlerts);
        Assert.Equal("Water heater", snap.WarrantyAlerts[0].AssetType);
        Assert.Equal("10", snap.WarrantyAlerts[0].UnitNumber);
        Assert.InRange(snap.WarrantyAlerts[0].DaysLeft, 1, 90);
    }

    private sealed class FixedClock : Wiley.Apartments.Contracts.IDateTimeService
    {
        private readonly DateTime _utc;
        public FixedClock(DateTime utc) => _utc = utc;
        public DateTime UtcNow => _utc;
        public DateTime ToDisplayTime(DateTime utc) => utc;
        public DateTime ToUtc(DateTime local) => local;
    }
}
