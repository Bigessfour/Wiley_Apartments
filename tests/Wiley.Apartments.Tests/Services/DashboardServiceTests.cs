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
        var paths = new Wiley.Apartments.Tests.Support.FixedDocumentPathResolver(opts.Value.DocumentRoot);
        var documents = new DocumentService(db, paths, clock, NullLogger<DocumentService>.Instance);
        var leases = new LeaseService(
            db, opts, paths, env, clock, new LeaseDocumentGenerator(), documents, NullLogger<LeaseService>.Instance);
        var rentRoll = new RentRollService(db, NullLogger<RentRollService>.Instance);
        var ops = new UnitOperatingCostService(db, NullLogger<UnitOperatingCostService>.Instance);
        var maintenance = new MaintenanceService(db, ops, clock, NullLogger<MaintenanceService>.Instance);
        var schedule = new ScheduleService(db, clock, NullLogger<ScheduleService>.Instance);
        var service = new DashboardService(db, rentRoll, leases, maintenance, schedule, clock);
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
            snap.OccupancyPercent.Should().Be(50);
            snap.UnitStatusSlices.Should().Contain(s => s.Status == "Occupied" && s.Count == 1);
            snap.CollectionByMonth.Should().HaveCount(12);
            snap.PaymentHeatmap.Should().NotBeNull();
            snap.CollectionRatePercent.Should().BeGreaterThanOrEqualTo(0);
            snap.ExpiringWarranties.Should().ContainSingle(w => w.AssetLabel.Contains("Fridge"));
            snap.OpenWorkOrders.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task GetSnapshotAsync_ComputesCollectionKpis_ExcludingDeposits()
    {
        var (db, service) = Create();
        await using (db)
        {
            var unit = new Unit
            {
                Id = Guid.NewGuid(),
                Number = "301",
                SqFt = 700,
                Beds = 2,
                Baths = 1,
                Status = UnitStatus.Occupied,
                MonthlyRent = 900m
            };
            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                FirstName = "Ada",
                LastName = "Clerk",
                IsDeleted = false
            };
            db.Units.Add(unit);
            db.Tenants.Add(tenant);
            db.LedgerEntries.AddRange(
                new LedgerEntry
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    UnitId = unit.Id,
                    EntryType = LedgerEntryType.Charge,
                    Amount = 1200m,
                    DateUtc = new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc)
                },
                new LedgerEntry
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    UnitId = unit.Id,
                    EntryType = LedgerEntryType.Payment,
                    Amount = 900m,
                    DateUtc = new DateTime(2026, 8, 5, 12, 0, 0, DateTimeKind.Utc),
                    IsDeposit = false
                },
                new LedgerEntry
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    UnitId = unit.Id,
                    EntryType = LedgerEntryType.Payment,
                    Amount = 500m,
                    DateUtc = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc),
                    IsDeposit = true
                },
                new LedgerEntry
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    UnitId = unit.Id,
                    EntryType = LedgerEntryType.Charge,
                    Amount = 500m,
                    DateUtc = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc),
                    IsDeposit = true,
                    Notes = "Security deposit charge"
                });
            await db.SaveChangesAsync();

            var snap = await service.GetSnapshotAsync();
            snap.ExpectedRentThisMonth.Should().Be(900m);
            snap.CollectedThisMonth.Should().Be(900m);
            // Aug charge 1200 − rent 900 − Jul deposit charge/payment net 0 → +300
            snap.OutstandingBalanceTotal.Should().Be(300m);
            snap.CollectionByMonth.Should().Contain(m => m.Label.Contains("Aug") && m.Amount == 900m);
            snap.CollectionByMonth.Should().Contain(m => m.Label.Contains("Jul") && m.Amount == 0m);
            snap.CollectionRatePercent.Should().Be(100);
            snap.PaymentHeatmap.Should().Contain(c => c.Unit == "301" && c.Month.Contains("Aug") && c.Value == 900);
        }
    }
}
