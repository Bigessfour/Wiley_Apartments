using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Data;
using Wiley.Apartments.Web.Services;

namespace Wiley.Apartments.Tests.Services;

public class OccupancyServiceTests
{
    private sealed class FixedClock : Wiley.Apartments.Contracts.IDateTimeService
    {
        public DateTime UtcNow { get; set; } = new(2026, 8, 9, 18, 0, 0, DateTimeKind.Utc);
        public DateTime ToDisplayTime(DateTime utc) => utc;
        public DateTime ToUtc(DateTime local) => local.ToUniversalTime();
    }

    private static (ApartmentsDbContext Db, OccupancyService Service, FixedClock Clock) Create()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApartmentsDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new ApartmentsDbContext(options);
        db.Database.EnsureCreated();
        var clock = new FixedClock();
        var service = new OccupancyService(db, clock, NullLogger<OccupancyService>.Instance);
        return (db, service, clock);
    }

    private static async Task<(Unit Unit, Tenant Tenant)> SeedAsync(ApartmentsDbContext db)
    {
        var unit = new Unit { Id = Guid.NewGuid(), Number = "3", SqFt = 700, Beds = 2, Baths = 1 };
        var tenant = new Tenant { Id = Guid.NewGuid(), FirstName = "Pat", LastName = "Nguyen" };
        db.Units.Add(unit);
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        return (unit, tenant);
    }

    [Fact]
    public async Task StartAsync_SetsOccupiedAndCurrentTenant()
    {
        var (db, service, _) = Create();
        await using (db)
        {
            var (unit, tenant) = await SeedAsync(db);

            var occupancy = await service.StartAsync(unit.Id, tenant.Id);

            occupancy.EndUtc.Should().BeNull();
            var reloaded = await db.Units.AsNoTracking().SingleAsync(u => u.Id == unit.Id);
            reloaded.Status.Should().Be(UnitStatus.Occupied);
            reloaded.CurrentTenantId.Should().Be(tenant.Id);
        }
    }

    [Fact]
    public async Task EndAsync_RetainsHistory_AndClearsUnit()
    {
        var (db, service, clock) = Create();
        await using (db)
        {
            var (unit, tenant) = await SeedAsync(db);
            await service.StartAsync(unit.Id, tenant.Id);
            clock.UtcNow = clock.UtcNow.AddDays(30);

            await service.EndAsync(unit.Id);

            var history = await service.GetHistoryForUnitAsync(unit.Id);
            history.Should().ContainSingle();
            history[0].EndUtc.Should().NotBeNull();
            (await service.GetCurrentForUnitAsync(unit.Id)).Should().BeNull();

            var reloaded = await db.Units.AsNoTracking().SingleAsync(u => u.Id == unit.Id);
            reloaded.Status.Should().Be(UnitStatus.MakeReady);
            reloaded.CurrentTenantId.Should().BeNull();
        }
    }

    [Fact]
    public async Task StartAsync_Throws_WhenUnitAlreadyOccupied()
    {
        var (db, service, _) = Create();
        await using (db)
        {
            var (unit, tenant) = await SeedAsync(db);
            await service.StartAsync(unit.Id, tenant.Id);
            var other = new Tenant { Id = Guid.NewGuid(), FirstName = "Sam", LastName = "Lee" };
            db.Tenants.Add(other);
            await db.SaveChangesAsync();

            var act = () => service.StartAsync(unit.Id, other.Id);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*active occupancy*");
        }
    }

    [Fact]
    public async Task StartAsync_Throws_WhenFacilityUnit()
    {
        var (db, service, _) = Create();
        await using (db)
        {
            var unit = new Unit
            {
                Id = Guid.NewGuid(),
                Number = "CC",
                IsFacility = true,
                SqFt = 2000,
                Status = UnitStatus.Vacant
            };
            var tenant = new Tenant { Id = Guid.NewGuid(), FirstName = "Pat", LastName = "Nguyen" };
            db.Units.Add(unit);
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();

            var act = () => service.StartAsync(unit.Id, tenant.Id);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*facility*");
        }
    }

    [Fact]
    public async Task StartAsync_SucceedsWhenTrackedUnitHasStaleRowVersion()
    {
        var (db, service, _) = Create();
        await using (db)
        {
            var (unit, tenant) = await SeedAsync(db);
            var tracked = await db.Units.FindAsync(unit.Id);
            tracked!.Notes = "circuit stale";
            db.Entry(tracked).Property(u => u.RowVersion).OriginalValue = Guid.NewGuid();

            var occupancy = await service.StartAsync(unit.Id, tenant.Id);

            occupancy.EndUtc.Should().BeNull();
            var reloaded = await db.Units.AsNoTracking().SingleAsync(u => u.Id == unit.Id);
            reloaded.Status.Should().Be(UnitStatus.Occupied);
            reloaded.CurrentTenantId.Should().Be(tenant.Id);
        }
    }
}
