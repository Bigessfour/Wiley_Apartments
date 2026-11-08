using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Data;
using Wiley.Apartments.Web.Services;

namespace Wiley.Apartments.Tests.Services;

public sealed class SharedContractOperationalEventExportServiceTests
{
    private static (ApartmentsDbContext Db, SharedContractOperationalEventExportService Sut) Create()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApartmentsDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new ApartmentsDbContext(options);
        db.Database.EnsureCreated();
        return (db, new SharedContractOperationalEventExportService(db, NullLogger<SharedContractOperationalEventExportService>.Instance));
    }

    [Fact]
    public async Task BuildForPeriodAsync_MapsOperatingCosts_ToContractChecks()
    {
        var (db, sut) = Create();
        await using (db)
        {
            var unit = new Unit { Id = Guid.NewGuid(), Number = "12", SqFt = 500, Beds = 1, Baths = 1 };
            db.Units.Add(unit);
            db.UnitOperatingCosts.Add(new UnitOperatingCost
            {
                Id = Guid.NewGuid(),
                UnitId = unit.Id,
                Category = OperatingCostCategory.Repair,
                Amount = 125.50m,
                IncurredUtc = new DateTime(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc),
                Vendor = "FAST HARDWARE",
                Notes = "faucet"
            });
            db.UnitOperatingCosts.Add(new UnitOperatingCost
            {
                Id = Guid.NewGuid(),
                UnitId = null,
                Category = OperatingCostCategory.CommonUpkeep,
                Amount = 40m,
                IncurredUtc = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                Vendor = "Outside period"
            });
            await db.SaveChangesAsync();

            var events = await sut.BuildForPeriodAsync("2026-10");

            events.Should().HaveCount(1);
            var ev = events[0];
            ev.Source.Should().Be("other");
            ev.Type.Should().Be("check");
            ev.Date.Should().Be("2026-10-15");
            ev.Amount.Should().Be(125.50m);
            ev.Description.Should().Contain("FAST HARDWARE");
            ev.Metadata["enterpriseId"].Should().Be(SharedContractOperationalEventExportService.EnterpriseId);
            ev.Metadata["publisherSystem"].Should().Be(SharedContractOperationalEventExportService.PublisherSystem);
            ev.Metadata["costCategory"].Should().Be("Repair");
        }
    }

    [Fact]
    public void TryParsePeriod_RejectsBadInput()
    {
        SharedContractOperationalEventExportService.TryParsePeriod("2026-13", out _, out _)
            .Should().BeFalse();
        SharedContractOperationalEventExportService.TryParsePeriod("2026-10", out var start, out var end)
            .Should().BeTrue();
        start.Should().Be(new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc));
        end.Should().Be(new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc));
    }
}
