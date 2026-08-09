using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Configuration;
using Wiley.Apartments.Web.Data;
using Wiley.Apartments.Web.Services;

namespace Wiley.Apartments.Tests.Services;

public class LedgerServiceTests
{
    private sealed class FixedClock : Wiley.Apartments.Contracts.IDateTimeService
    {
        public DateTime UtcNow { get; set; } = new(2026, 8, 20, 12, 0, 0, DateTimeKind.Utc);
        public DateTime ToDisplayTime(DateTime utc) => utc;
        public DateTime ToUtc(DateTime local) => DateTime.SpecifyKind(local, DateTimeKind.Utc);
    }

    private static (ApartmentsDbContext Db, LedgerService Service, LateFeeSettingsService LateFees, FixedClock Clock) Create()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApartmentsDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new ApartmentsDbContext(options);
        db.Database.EnsureCreated();
        var lateFees = new LateFeeSettingsService(
            db,
            Options.Create(new ClerkSuiteOptions()));
        var clock = new FixedClock();
        var service = new LedgerService(db, lateFees, clock, NullLogger<LedgerService>.Instance);
        return (db, service, lateFees, clock);
    }

    private static async Task<(Unit Unit, Tenant Tenant)> SeedAsync(ApartmentsDbContext db)
    {
        var unit = new Unit { Id = Guid.NewGuid(), Number = "5", SqFt = 500, Beds = 1, Baths = 1 };
        var tenant = new Tenant { Id = Guid.NewGuid(), FirstName = "Sam", LastName = "Lee" };
        db.Units.Add(unit);
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        return (unit, tenant);
    }

    [Fact]
    public async Task PostChargeAndPayment_ComputesRunningBalance()
    {
        var (db, service, _, _) = Create();
        await using (db)
        {
            var (unit, tenant) = await SeedAsync(db);
            var day1 = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
            var day2 = new DateTime(2026, 8, 5, 0, 0, 0, DateTimeKind.Utc);

            await service.PostChargeAsync(tenant.Id, unit.Id, 650m, day1, notes: "August rent");
            await service.PostPaymentAsync(
                tenant.Id, unit.Id, 400m, day2, PaymentMethod.Cash, notes: "Partial");

            var lines = await service.GetLedgerAsync(tenant.Id, unit.Id);
            lines.Should().HaveCount(2);
            lines[0].RunningBalance.Should().Be(650m);
            lines[1].RunningBalance.Should().Be(250m);
            (await service.GetBalanceAsync(tenant.Id, unit.Id)).Should().Be(250m);
        }
    }

    [Fact]
    public async Task SoftDelete_RemovesFromBalance()
    {
        var (db, service, _, _) = Create();
        await using (db)
        {
            var (unit, tenant) = await SeedAsync(db);
            var charge = await service.PostChargeAsync(
                tenant.Id, unit.Id, 100m, DateTime.UtcNow);
            await service.PostPaymentAsync(
                tenant.Id, unit.Id, 100m, DateTime.UtcNow, PaymentMethod.Check);

            await service.SoftDeleteAsync(charge.Id);

            (await service.GetBalanceAsync(tenant.Id)).Should().Be(-100m);
            (await service.GetLedgerAsync(tenant.Id)).Should().HaveCount(1);
        }
    }

    [Fact]
    public async Task PostCharge_RejectsNonPositiveAmount()
    {
        var (db, service, _, _) = Create();
        await using (db)
        {
            var (unit, tenant) = await SeedAsync(db);
            var act = () => service.PostChargeAsync(tenant.Id, unit.Id, 0m, DateTime.UtcNow);
            await act.Should().ThrowAsync<ArgumentException>();
        }
    }

    [Fact]
    public async Task ApplyLateFeesAsync_PostsWhenEnabledAndPastGrace()
    {
        var (db, service, lateFees, clock) = Create();
        await using (db)
        {
            var (unit, tenant) = await SeedAsync(db);
            await lateFees.UpdateAsync(enabled: true, amount: 25m, graceDays: 5);
            await service.PostChargeAsync(
                tenant.Id, unit.Id, 650m, clock.UtcNow.AddDays(-10), notes: "Rent");

            var count = await service.ApplyLateFeesAsync(clock.UtcNow);
            count.Should().Be(1);
            (await service.GetBalanceAsync(tenant.Id, unit.Id)).Should().Be(675m);

            // Second run same month — no duplicate
            (await service.ApplyLateFeesAsync(clock.UtcNow)).Should().Be(0);
        }
    }

    [Fact]
    public async Task ApplyLateFeesAsync_SkipsWhenDisabled()
    {
        var (db, service, lateFees, clock) = Create();
        await using (db)
        {
            var (unit, tenant) = await SeedAsync(db);
            await lateFees.UpdateAsync(enabled: false, amount: 25m, graceDays: 0);
            await service.PostChargeAsync(tenant.Id, unit.Id, 100m, clock.UtcNow.AddDays(-30));
            (await service.ApplyLateFeesAsync(clock.UtcNow)).Should().Be(0);
        }
    }

    [Fact]
    public async Task ApplyLateFeesAsync_SkipsWhenChargeStillWithinGrace()
    {
        var (db, service, lateFees, clock) = Create();
        await using (db)
        {
            var (unit, tenant) = await SeedAsync(db);
            await lateFees.UpdateAsync(enabled: true, amount: 25m, graceDays: 10);
            await service.PostChargeAsync(tenant.Id, unit.Id, 650m, clock.UtcNow.AddDays(-3));
            (await service.ApplyLateFeesAsync(clock.UtcNow)).Should().Be(0);
            (await service.GetBalanceAsync(tenant.Id, unit.Id)).Should().Be(650m);
        }
    }
}
