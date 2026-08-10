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
    private static UnitService CreateService(ApartmentsDbContext db, int maxUnits = 0)
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
    public async Task CreateAsync_AllowsBeyondSixteen_WhenUnlimited()
    {
        await using var db = CreateContext();
        var service = CreateService(db, maxUnits: 0);
        for (var i = 1; i <= 17; i++)
        {
            await service.CreateAsync(new Unit { Number = i.ToString(), SqFt = 500, Beds = 1, Baths = 1 });
        }

        (await service.CountAsync()).Should().Be(17);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenMaxUnitsReached()
    {
        await using var db = CreateContext();
        var service = CreateService(db, maxUnits: 1);
        await service.CreateAsync(new Unit { Number = "1", SqFt = 500, Beds = 1, Baths = 1 });

        var act = () => service.CreateAsync(new Unit { Number = "2", SqFt = 500, Beds = 1, Baths = 1 });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*more than 1 residential units*");
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

    [Fact]
    public async Task CreateAsync_Facility_DoesNotConsumeResidentialCap()
    {
        await using var db = CreateContext();
        var service = CreateService(db, maxUnits: 1);
        await service.CreateAsync(new Unit { Number = "1", SqFt = 500, Beds = 1, Baths = 1 });

        var facility = await service.CreateAsync(new Unit
        {
            Number = "CC",
            IsFacility = true,
            SqFt = 2000,
            Beds = 0,
            Baths = 2,
            Notes = "Community Center"
        });

        facility.IsFacility.Should().BeTrue();
        facility.Number.Should().Be("CC");
        (await service.CountAsync()).Should().Be(1);
        (await service.GetAllAsync()).Should().HaveCount(2);
    }

    [Fact]
    public async Task GetFacilityAsync_ReturnsCommunityCenter()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        await service.CreateAsync(new Unit { Number = "1", SqFt = 500, Beds = 1, Baths = 1 });
        await service.CreateAsync(new Unit { Number = "CC", IsFacility = true, SqFt = 1000 });

        var facility = await service.GetFacilityAsync();
        facility.Should().NotBeNull();
        facility!.Number.Should().Be("CC");
        facility.IsFacility.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_Throws_WhenFacility()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        var facility = await service.CreateAsync(new Unit { Number = "CC", IsFacility = true, SqFt = 1000 });

        var act = () => service.DeleteAsync(facility.Id);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*facility*");
    }

    [Fact]
    public async Task UpdateAsync_PersistsRentDepositAndHandicap()
    {
        await using var db = CreateContext();
        var service = CreateService(db, maxUnits: 0);
        var unit = await service.CreateAsync(new Unit
        {
            Number = "301",
            SqFt = 900,
            Beds = 3,
            Baths = 1
        });

        unit.MonthlyRent = 900m;
        unit.SecurityDeposit = 900m;
        unit.IsHandicapAccessible = true;
        unit.LeaseTerm = "Year";
        var updated = await service.UpdateAsync(unit);

        updated.MonthlyRent.Should().Be(900m);
        updated.SecurityDeposit.Should().Be(900m);
        updated.IsHandicapAccessible.Should().BeTrue();
        updated.LeaseTerm.Should().Be("Year");
    }
}
