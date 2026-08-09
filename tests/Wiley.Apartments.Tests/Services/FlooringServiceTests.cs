using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Data;
using Wiley.Apartments.Web.Services;

namespace Wiley.Apartments.Tests.Services;

public class FlooringServiceTests
{
    private static FlooringService CreateService(ApartmentsDbContext db) =>
        new(db, NullLogger<FlooringService>.Instance);

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
    public async Task GetByUnitIdAsync_OrdersByInstallDateDescending()
    {
        await using var db = CreateContext();
        var unitId = Guid.NewGuid();
        db.Units.Add(new Unit { Id = unitId, Number = "1", SqFt = 500, Beds = 1, Baths = 1 });
        db.Floorings.AddRange(
            new Flooring
            {
                Id = Guid.NewGuid(),
                UnitId = unitId,
                Type = "Carpet",
                InstallDate = new DateOnly(2018, 3, 1)
            },
            new Flooring
            {
                Id = Guid.NewGuid(),
                UnitId = unitId,
                Type = "Vinyl plank",
                InstallDate = new DateOnly(2023, 6, 15),
                ReplacedDate = new DateOnly(2023, 6, 14)
            });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var results = await service.GetByUnitIdAsync(unitId);

        results.Should().HaveCount(2);
        results[0].InstallDate.Should().Be(new DateOnly(2023, 6, 15));
    }

    [Fact]
    public async Task CreateAsync_StoresReplacementHistory()
    {
        await using var db = CreateContext();
        var unitId = Guid.NewGuid();
        db.Units.Add(new Unit { Id = unitId, Number = "2", SqFt = 600, Beds = 2, Baths = 1 });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var created = await service.CreateAsync(new Flooring
        {
            UnitId = unitId,
            Type = "Berber carpet",
            InstallDate = new DateOnly(2015, 1, 10),
            Condition = "Worn",
            ReplacedDate = new DateOnly(2022, 8, 1),
            Notes = "Replaced with LVP in living areas"
        });

        created.ReplacedDate.Should().Be(new DateOnly(2022, 8, 1));
        created.Notes.Should().Contain("LVP");
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenReplacedBeforeInstall()
    {
        await using var db = CreateContext();
        var unitId = Guid.NewGuid();
        db.Units.Add(new Unit { Id = unitId, Number = "3", SqFt = 650, Beds = 2, Baths = 1 });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var act = () => service.CreateAsync(new Flooring
        {
            UnitId = unitId,
            Type = "Tile",
            InstallDate = new DateOnly(2024, 5, 1),
            ReplacedDate = new DateOnly(2023, 1, 1)
        });

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Replaced date*");
    }
}
