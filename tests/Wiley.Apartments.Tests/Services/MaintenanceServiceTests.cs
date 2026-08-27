using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Data;
using Wiley.Apartments.Web.Services;

namespace Wiley.Apartments.Tests.Services;

public class MaintenanceServiceTests
{
    private sealed class FixedClock : Wiley.Apartments.Contracts.IDateTimeService
    {
        public DateTime UtcNow { get; set; } = new(2026, 8, 9, 18, 0, 0, DateTimeKind.Utc);
        public DateTime ToDisplayTime(DateTime utc) => utc;
        public DateTime ToUtc(DateTime local) => DateTime.SpecifyKind(local, DateTimeKind.Utc);
    }

    private static (ApartmentsDbContext Db, MaintenanceService Service) Create()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApartmentsDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new ApartmentsDbContext(options);
        db.Database.EnsureCreated();
        var clock = new FixedClock();
        var ops = new UnitOperatingCostService(db, NullLogger<UnitOperatingCostService>.Instance);
        var service = new MaintenanceService(db, ops, clock, NullLogger<MaintenanceService>.Instance);
        return (db, service);
    }

    private static async Task<(Unit Unit, Asset Asset)> SeedAsync(ApartmentsDbContext db)
    {
        var unit = new Unit { Id = Guid.NewGuid(), Number = "12", SqFt = 600, Beds = 2, Baths = 1 };
        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            UnitId = unit.Id,
            Type = "Fridge",
            Serial = "FR-1",
            Make = "GE"
        };
        db.Units.Add(unit);
        db.Assets.Add(asset);
        await db.SaveChangesAsync();
        return (unit, asset);
    }

    [Fact]
    public async Task CreateAndGetForUnitAndAsset()
    {
        var (db, service) = Create();
        await using (db)
        {
            var (unit, asset) = await SeedAsync(db);
            var wo = await service.CreateAsync(
                unit.Id,
                "Not cooling",
                MaintenancePriority.High,
                asset.Id);

            wo.Status.Should().Be(MaintenanceStatus.Open);
            (await service.GetForUnitAsync(unit.Id)).Should().ContainSingle();
            (await service.GetForAssetAsync(asset.Id)).Should().ContainSingle(m => m.Id == wo.Id);
        }
    }

    [Fact]
    public async Task CompleteAsync_PostsRepairOperatingCost()
    {
        var (db, service) = Create();
        await using (db)
        {
            var (unit, asset) = await SeedAsync(db);
            var wo = await service.CreateAsync(unit.Id, "Fix sink", MaintenancePriority.Normal, asset.Id);
            var done = await service.CompleteAsync(wo.Id, cost: 85.50m, completedByDisplay: "Test Clerk");

            done.Status.Should().Be(MaintenanceStatus.Completed);
            done.Cost.Should().Be(85.50m);
            done.OperatingCostId.Should().NotBeNull();

            var ops = await db.UnitOperatingCosts.AsNoTracking()
                .SingleAsync(c => c.Id == done.OperatingCostId);
            ops.Category.Should().Be(OperatingCostCategory.Repair);
            ops.Amount.Should().Be(85.50m);
            ops.MaintenanceRequestId.Should().Be(wo.Id);
            ops.UnitId.Should().Be(unit.Id);
            ops.Notes.Should().Contain("Fridge");
        }
    }

    [Fact]
    public async Task CompleteAsync_SkipsOpsCost_WhenZero()
    {
        var (db, service) = Create();
        await using (db)
        {
            var (unit, _) = await SeedAsync(db);
            var wo = await service.CreateAsync(unit.Id, "Tighten screw");
            var done = await service.CompleteAsync(wo.Id, cost: 0m, completedByDisplay: "Test Clerk");
            done.OperatingCostId.Should().BeNull();
            (await db.UnitOperatingCosts.CountAsync()).Should().Be(0);
        }
    }

    [Fact]
    public async Task CompleteAsync_RequiresCompletedByDisplay()
    {
        var (db, service) = Create();
        await using (db)
        {
            var (unit, _) = await SeedAsync(db);
            var wo = await service.CreateAsync(unit.Id, "Fix sink");

            var act = async () => await service.CompleteAsync(wo.Id, cost: 10m, completedByDisplay: "  ");

            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Completed by*");
        }
    }
}
