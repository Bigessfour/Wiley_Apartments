using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Wiley.Apartments.Contracts;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Configuration;
using Wiley.Apartments.Web.Data;
using Wiley.Apartments.Web.Services;

namespace Wiley.Apartments.Tests.Services;

public class DashboardServiceTests
{
    private sealed class FixedClock : IDateTimeService
    {
        public DateTime UtcNow { get; set; } = new(2026, 8, 9, 18, 0, 0, DateTimeKind.Utc);
        public DateTime ToDisplayTime(DateTime utc) => utc;
        public DateTime ToUtc(DateTime local) => DateTime.SpecifyKind(local, DateTimeKind.Utc);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "tests";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    private static (ApartmentsDbContext Db, DashboardService Service) Create()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApartmentsDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new ApartmentsDbContext(options);
        db.Database.EnsureCreated();
        var clock = new FixedClock();
        var env = new TestHostEnvironment();
        var opts = Options.Create(new ClerkSuiteOptions { DocumentRoot = Path.GetTempPath() });
        var documents = new DocumentService(db, opts, env, clock, NullLogger<DocumentService>.Instance);
        var leases = new LeaseService(
            db, opts, env, clock, new LeaseDocumentGenerator(), documents, NullLogger<LeaseService>.Instance);
        var rentRoll = new RentRollService(db);
        var ops = new UnitOperatingCostService(db, NullLogger<UnitOperatingCostService>.Instance);
        var maintenance = new MaintenanceService(db, ops, clock, NullLogger<MaintenanceService>.Instance);
        var service = new DashboardService(db, rentRoll, leases, maintenance, clock);
        return (db, service);
    }

    [Fact]
    public async Task GetSnapshotAsync_CountsUnitStatusesAndWarranties()
    {
        var (db, service) = Create();
        await using (db)
        {
            var occupied = new Unit
            {
                Id = Guid.NewGuid(),
                Number = "1",
                SqFt = 500,
                Beds = 1,
                Baths = 1,
                Status = UnitStatus.Occupied
            };
            var vacant = new Unit
            {
                Id = Guid.NewGuid(),
                Number = "2",
                SqFt = 500,
                Beds = 1,
                Baths = 1,
                Status = UnitStatus.Vacant
            };
            db.Units.AddRange(occupied, vacant);
            db.Assets.Add(new Asset
            {
                Id = Guid.NewGuid(),
                UnitId = occupied.Id,
                Type = "Fridge",
                Serial = "F-1",
                WarrantyEnd = new DateOnly(2026, 9, 1)
            });
            await db.SaveChangesAsync();

            var snap = await service.GetSnapshotAsync();
            snap.TotalUnits.Should().Be(2);
            snap.Occupied.Should().Be(1);
            snap.Vacant.Should().Be(1);
            snap.ExpiringWarranties.Should().ContainSingle(w => w.AssetLabel.Contains("Fridge"));
            snap.OpenWorkOrders.Should().BeEmpty();
        }
    }
}
