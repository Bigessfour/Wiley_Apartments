using Microsoft.EntityFrameworkCore;
using Wiley.Apartments.Contracts;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Data;
using Wiley.Apartments.Web.Services;

namespace Wiley.Apartments.Tests.Services;

public class PortfolioProfitLossServiceTests
{
    private sealed class FixedClock : IDateTimeService
    {
        public DateTime UtcNow { get; set; } = new(2026, 8, 15, 12, 0, 0, DateTimeKind.Utc);
        public DateTime ToDisplayTime(DateTime utc) => utc;
        public DateTime ToUtc(DateTime local) => DateTime.SpecifyKind(local, DateTimeKind.Utc);
    }

    private static (ApartmentsDbContext Db, PortfolioProfitLossService Service, FixedClock Clock) Create()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApartmentsDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new ApartmentsDbContext(options);
        db.Database.EnsureCreated();
        var clock = new FixedClock();
        return (db, new PortfolioProfitLossService(db, clock), clock);
    }

    [Fact]
    public async Task GetAsync_EmptyPortfolio_ReturnsZeroTotals()
    {
        var (db, service, _) = Create();
        await using (db)
        {
            var report = await service.GetAsync(ProfitLossPeriod.YearToDate);
            report.TotalIncome.Should().Be(0);
            report.TotalExpense.Should().Be(0);
            report.NetIncome.Should().Be(0);
            report.ByUnit.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task GetAsync_SumsPaymentsAndCosts_AllocatesCommonUpkeep()
    {
        var (db, service, _) = Create();
        await using (db)
        {
            var unitA = new Unit { Id = Guid.NewGuid(), Number = "1", SqFt = 500, Beds = 1, Baths = 1 };
            var unitB = new Unit { Id = Guid.NewGuid(), Number = "2", SqFt = 500, Beds = 1, Baths = 1 };
            var tenant = new Tenant { Id = Guid.NewGuid(), FirstName = "A", LastName = "B" };
            db.Units.AddRange(unitA, unitB);
            db.Tenants.Add(tenant);
            db.LedgerEntries.Add(new LedgerEntry
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                UnitId = unitA.Id,
                EntryType = LedgerEntryType.Payment,
                Amount = 1000m,
                DateUtc = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)
            });
            db.UnitOperatingCosts.AddRange(
                new UnitOperatingCost
                {
                    Id = Guid.NewGuid(),
                    UnitId = unitA.Id,
                    Category = OperatingCostCategory.Repair,
                    Amount = 100m,
                    IncurredUtc = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new UnitOperatingCost
                {
                    Id = Guid.NewGuid(),
                    UnitId = null,
                    Category = OperatingCostCategory.CommonUpkeep,
                    Amount = 200m,
                    IncurredUtc = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc)
                });
            await db.SaveChangesAsync();

            var report = await service.GetAsync(ProfitLossPeriod.YearToDate);
            report.TotalIncome.Should().Be(1000m);
            report.TotalExpense.Should().Be(300m);
            report.NetIncome.Should().Be(700m);

            var a = report.ByUnit.Single(u => u.UnitId == unitA.Id);
            a.Income.Should().Be(1000m);
            a.Expense.Should().Be(200m); // 100 repair + 100 allocated common
            a.Net.Should().Be(800m);

            var b = report.ByUnit.Single(u => u.UnitId == unitB.Id);
            b.Income.Should().Be(0m);
            b.Expense.Should().Be(100m);
            b.Net.Should().Be(-100m);

            report.Series.Should().NotBeEmpty();
        }
    }

    [Fact]
    public async Task GetAsync_MonthPeriod_UsesDailySeries()
    {
        var (db, service, clock) = Create();
        await using (db)
        {
            clock.UtcNow = new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc);
            var report = await service.GetAsync(ProfitLossPeriod.Month);
            report.Period.Should().Be(ProfitLossPeriod.Month);
            report.Series.Count.Should().BeGreaterThanOrEqualTo(10);
        }
    }
}
