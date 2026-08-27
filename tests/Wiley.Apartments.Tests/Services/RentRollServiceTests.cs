using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Wiley.Apartments.Contracts;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Data;
using Wiley.Apartments.Web.Services;

namespace Wiley.Apartments.Tests.Services;

public class RentRollServiceTests
{
    private static (ApartmentsDbContext Db, RentRollService Service) Create()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApartmentsDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new ApartmentsDbContext(options);
        db.Database.EnsureCreated();
        return (db, new RentRollService(db, NullLogger<RentRollService>.Instance));
    }

    [Fact]
    public async Task GetDelinquencyAsync_Default_CurrentOccupantsOnly()
    {
        var (db, service) = Create();
        await using (db)
        {
            var unit = new Unit { Id = Guid.NewGuid(), Number = "1", SqFt = 500, Beds = 1, Baths = 1, Status = UnitStatus.Occupied };
            var current = new Tenant { Id = Guid.NewGuid(), FirstName = "Ada", LastName = "Now" };
            var former = new Tenant { Id = Guid.NewGuid(), FirstName = "Pat", LastName = "Then" };
            unit.CurrentTenantId = current.Id;
            db.Units.Add(unit);
            db.Tenants.AddRange(current, former);
            db.Occupancies.AddRange(
                new Occupancy
                {
                    Id = Guid.NewGuid(),
                    UnitId = unit.Id,
                    TenantId = former.Id,
                    StartUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    EndUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Occupancy
                {
                    Id = Guid.NewGuid(),
                    UnitId = unit.Id,
                    TenantId = current.Id,
                    StartUtc = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                    EndUtc = null
                });
            db.LedgerEntries.AddRange(
                new LedgerEntry
                {
                    Id = Guid.NewGuid(),
                    TenantId = former.Id,
                    UnitId = unit.Id,
                    EntryType = LedgerEntryType.Charge,
                    Amount = 4000m,
                    DateUtc = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new LedgerEntry
                {
                    Id = Guid.NewGuid(),
                    TenantId = current.Id,
                    UnitId = unit.Id,
                    EntryType = LedgerEntryType.Charge,
                    Amount = 250m,
                    DateUtc = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc)
                });
            await db.SaveChangesAsync();

            var currentRows = await service.GetDelinquencyAsync();
            currentRows.Should().ContainSingle(r => r.TenantName == "Now, Ada" && r.Balance == 250m);

            var formerRows = await service.GetDelinquencyAsync(OccupancyFilter.Former);
            formerRows.Should().ContainSingle(r => r.TenantName == "Then, Pat" && r.Balance == 4000m);

            var all = await service.GetDelinquencyAsync(OccupancyFilter.All);
            all.Should().HaveCount(2);
        }
    }

    [Fact]
    public async Task GetDelinquencyAsync_Current_ExcludesVacantUnitLeftoverRoster()
    {
        var (db, service) = Create();
        await using (db)
        {
            var unit = new Unit
            {
                Id = Guid.NewGuid(),
                Number = "312",
                SqFt = 500,
                Beds = 1,
                Baths = 1,
                Status = UnitStatus.Vacant
            };
            var former = new Tenant { Id = Guid.NewGuid(), FirstName = "Jeanette", LastName = "OBryan" };
            unit.CurrentTenantId = former.Id;
            db.Units.Add(unit);
            db.Tenants.Add(former);
            db.Occupancies.Add(new Occupancy
            {
                Id = Guid.NewGuid(),
                UnitId = unit.Id,
                TenantId = former.Id,
                StartUtc = new DateTime(2012, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                EndUtc = new DateTime(2025, 7, 30, 0, 0, 0, DateTimeKind.Utc)
            });
            db.LedgerEntries.Add(new LedgerEntry
            {
                Id = Guid.NewGuid(),
                TenantId = former.Id,
                UnitId = unit.Id,
                EntryType = LedgerEntryType.Charge,
                Amount = 1200m,
                DateUtc = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc)
            });
            await db.SaveChangesAsync();

            (await service.GetDelinquencyAsync(OccupancyFilter.Current)).Should().BeEmpty();
            (await service.GetDelinquencyAsync(OccupancyFilter.Former))
                .Should().ContainSingle(r => r.Balance == 1200m);
        }
    }

    [Fact]
    public async Task GetRentRollAsync_ExcludesCommunityCenter()
    {
        var (db, service) = Create();
        await using (db)
        {
            db.Units.AddRange(
                new Unit { Id = Guid.NewGuid(), Number = "1", SqFt = 500, Beds = 1, Baths = 1, Status = UnitStatus.Vacant },
                new Unit
                {
                    Id = Guid.NewGuid(),
                    Number = "CC",
                    IsFacility = true,
                    SqFt = 2000,
                    Status = UnitStatus.Vacant
                });
            await db.SaveChangesAsync();

            var rows = await service.GetRentRollAsync();
            rows.Should().ContainSingle(r => r.UnitNumber == "1");
            rows.Should().NotContain(r => r.UnitNumber == "CC");
        }
    }
}
