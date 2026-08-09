using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Configuration;
using Wiley.Apartments.Web.Data;
using Wiley.Apartments.Web.Services;

namespace Wiley.Apartments.Tests.Services;

public class UnitServiceTests
{
    private static UnitService CreateService(ApartmentsDbContext db, int maxUnits = 16)
    {
        var options = Options.Create(new ClerkSuiteOptions { MaxUnits = maxUnits });
        return new UnitService(db, options, NullLogger<UnitService>.Instance);
    }

    private static ApartmentsDbContext CreateContext()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApartmentsDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new ApartmentsDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenMaxUnitsReached()
    {
        await using var db = CreateContext();
        var service = CreateService(db, maxUnits: 1);
        await service.CreateAsync(new Unit { Number = "1", SqFt = 500, Beds = 1, Baths = 1 });

        var act = () => service.CreateAsync(new Unit { Number = "2", SqFt = 500, Beds = 1, Baths = 1 });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*more than 1 units*");
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenNumberDuplicate()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        await service.CreateAsync(new Unit { Number = "5", SqFt = 600, Beds = 2, Baths = 1 });

        var act = () => service.CreateAsync(new Unit { Number = "5", SqFt = 700, Beds = 2, Baths = 2 });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task DeleteAsync_Throws_WhenOccupied()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        var unit = await service.CreateAsync(new Unit
        {
            Number = "3",
            SqFt = 650,
            Beds = 2,
            Baths = 1,
            Status = UnitStatus.Occupied
        });

        var act = () => service.DeleteAsync(unit.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*occupied*");
    }

    [Fact]
    public async Task UpdateAsync_PersistsStatusChange()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        var unit = await service.CreateAsync(new Unit
        {
            Number = "7",
            SqFt = 720,
            Beds = 2,
            Baths = 1,
            Status = UnitStatus.Vacant
        });

        unit.Status = UnitStatus.Maintenance;
        var updated = await service.UpdateAsync(unit);

        updated.Status.Should().Be(UnitStatus.Maintenance);
        (await service.GetByIdAsync(unit.Id))!.Status.Should().Be(UnitStatus.Maintenance);
    }
}
