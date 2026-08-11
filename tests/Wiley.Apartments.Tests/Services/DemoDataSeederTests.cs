
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Tests.Support;
using Wiley.Apartments.Web.Configuration;
using Wiley.Apartments.Web.Data;
using Wiley.Apartments.Web.Services;

namespace Wiley.Apartments.Tests.Services;

public class DemoDataSeederTests
{
    private sealed class FixedClock : Wiley.Apartments.Contracts.IDateTimeService
    {
        public DateTime UtcNow { get; set; } = new(2026, 8, 10, 15, 0, 0, DateTimeKind.Utc);
        public DateTime ToDisplayTime(DateTime utc) => utc;
        public DateTime ToUtc(DateTime local) => DateTime.SpecifyKind(local, DateTimeKind.Utc);
    }

    private static (ApartmentsDbContext Db, DemoDataSeeder Seeder, string Root) Create()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApartmentsDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new ApartmentsDbContext(options);
        db.Database.EnsureCreated();
        var root = Path.Combine(Path.GetTempPath(), "clerksuite-demo-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var paths = new FixedDocumentPathResolver(root);
        var unitSeeder = new UnitSeeder(db, Options.Create(new ClerkSuiteOptions { MaxUnits = 16 }), NullLogger<UnitSeeder>.Instance);
        var seeder = new DemoDataSeeder(db, paths, new FixedClock(), unitSeeder, NullLogger<DemoDataSeeder>.Instance);
        return (db, seeder, root);
    }

    [Fact]
    public async Task SeedAsync_CreatesPrimaryResidentWith24MonthsAndCommunityCenter()
    {
        var (db, seeder, root) = Create();
        await using (db)
        {
            var result = await seeder.SeedAsync();
            result.AlreadyLoaded.Should().BeFalse();
            result.CommunityCenterRenters.Should().Be(5);
            result.LedgerEntries.Should().BeGreaterThan(40);

            var jordan = await db.Tenants.Include(t => t.HouseholdMembers).Include(t => t.Vehicles).Include(t => t.Pets)
                .FirstAsync(t => t.Email == DemoDataSeeder.PrimaryEmail);
            jordan.HouseholdMembers.Should().HaveCountGreaterThanOrEqualTo(2);
            jordan.Vehicles.Should().NotBeEmpty();
            jordan.Pets.Should().NotBeEmpty();

            var charges = await db.LedgerEntries.CountAsync(e =>
                e.TenantId == jordan.Id && e.EntryType == LedgerEntryType.Charge && !e.IsDeleted);
            charges.Should().BeGreaterThanOrEqualTo(25);

            var cc = await db.Units.FirstAsync(u => u.IsFacility);
            (await db.FacilityRenters.CountAsync(r => !r.IsDeleted)).Should().Be(5);
            (await db.FacilityReservations.CountAsync(r => r.UnitId == cc.Id)).Should().Be(5);
            (await db.FacilityInventoryItems.CountAsync(i => i.UnitId == cc.Id && !i.IsDeleted)).Should().BeGreaterThanOrEqualTo(6);

            var report = await seeder.ValidateAsync();
            report.Pass.Should().BeTrue(string.Join("; ", report.Checks.Where(c => !c.Pass).Select(c => c.Area + ": " + c.Detail)));

            var again = await seeder.SeedAsync();
            again.AlreadyLoaded.Should().BeTrue();
        }

        try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
    }

    [Fact]
    public async Task SeedAsync_Force_ReseedsCleanly()
    {
        var (db, seeder, root) = Create();
        await using (db)
        {
            await seeder.SeedAsync();
            var firstCount = await db.Tenants.CountAsync(t => t.Notes != null && t.Notes.Contains(DemoDataSeeder.DemoTag));
            await seeder.SeedAsync(force: true);
            var second = await db.Tenants.CountAsync(t => t.Notes != null && t.Notes.Contains(DemoDataSeeder.DemoTag));
            second.Should().Be(firstCount);
            (await seeder.ValidateAsync()).Pass.Should().BeTrue();
        }

        try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
    }
}
