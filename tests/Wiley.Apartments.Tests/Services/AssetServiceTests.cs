using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Data;
using Wiley.Apartments.Web.Services;

namespace Wiley.Apartments.Tests.Services;

public class AssetServiceTests
{
    private static AssetService CreateService(ApartmentsDbContext db) =>
        new(db, NullLogger<AssetService>.Instance);

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
    public async Task SearchBySerialAsync_FindsPartialMatch()
    {
        await using var db = CreateContext();
        var unitId = Guid.NewGuid();
        db.Units.Add(new Unit { Id = unitId, Number = "1", SqFt = 500, Beds = 1, Baths = 1 });
        db.Assets.Add(new Asset
        {
            Id = Guid.NewGuid(),
            UnitId = unitId,
            Type = "Refrigerator",
            Serial = "SN-ABC-12345",
            Make = "GE",
            Model = "GTS"
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var results = await service.SearchBySerialAsync("abc-12");

        results.Should().ContainSingle();
        results[0].Serial.Should().Be("SN-ABC-12345");
    }

    [Fact]
    public async Task CreateAsync_StoresWarrantyDates()
    {
        await using var db = CreateContext();
        var unitId = Guid.NewGuid();
        db.Units.Add(new Unit { Id = unitId, Number = "2", SqFt = 600, Beds = 2, Baths = 1 });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var created = await service.CreateAsync(new Asset
        {
            UnitId = unitId,
            Type = "Range",
            Serial = "RNG-001",
            Make = "Whirlpool",
            Model = "WFE",
            WarrantyStart = new DateOnly(2024, 1, 1),
            WarrantyEnd = new DateOnly(2026, 1, 1)
        });

        created.WarrantyStart.Should().Be(new DateOnly(2024, 1, 1));
        created.WarrantyEnd.Should().Be(new DateOnly(2026, 1, 1));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenDuplicateSerialOnUnit()
    {
        await using var db = CreateContext();
        var unitId = Guid.NewGuid();
        db.Units.Add(new Unit { Id = unitId, Number = "3", SqFt = 650, Beds = 2, Baths = 1 });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.CreateAsync(new Asset
        {
            UnitId = unitId,
            Type = "Dishwasher",
            Serial = "DW-100",
            Make = "Bosch",
            Model = "300"
        });

        var act = () => service.CreateAsync(new Asset
        {
            UnitId = unitId,
            Type = "Microwave",
            Serial = "DW-100",
            Make = "Samsung",
            Model = "ME"
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*serial*");
    }

    [Fact]
    public async Task SearchBySerialAsync_TreatsWildcardsAsLiterals()
    {
        await using var db = CreateContext();
        var unitId = Guid.NewGuid();
        db.Units.Add(new Unit { Id = unitId, Number = "4", SqFt = 650, Beds = 2, Baths = 1 });
        db.Assets.Add(new Asset
        {
            Id = Guid.NewGuid(),
            UnitId = unitId,
            Type = "HVAC",
            Serial = "SN-100%",
            Make = "Carrier",
            Model = "X"
        });
        db.Assets.Add(new Asset
        {
            Id = Guid.NewGuid(),
            UnitId = unitId,
            Type = "HVAC",
            Serial = "SN-1000",
            Make = "Carrier",
            Model = "Y"
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var results = await service.SearchBySerialAsync("100%");

        results.Should().ContainSingle();
        results[0].Serial.Should().Be("SN-100%");
    }
}
