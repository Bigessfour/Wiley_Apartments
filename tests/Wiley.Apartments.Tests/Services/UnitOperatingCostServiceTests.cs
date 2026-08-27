using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Data;
using Wiley.Apartments.Web.Services;

namespace Wiley.Apartments.Tests.Services;

public class UnitOperatingCostServiceTests
{
    private static (ApartmentsDbContext Db, UnitOperatingCostService Service) Create()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApartmentsDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new ApartmentsDbContext(options);
        db.Database.EnsureCreated();
        return (db, new UnitOperatingCostService(db, NullLogger<UnitOperatingCostService>.Instance));
    }

    [Fact]
    public async Task CreateAsync_RequiresUnit_ExceptCommonUpkeep()
    {
        var (db, service) = Create();
        await using (db)
        {
            var act = () => service.CreateAsync(
                OperatingCostCategory.Utility,
                40m,
                DateTime.UtcNow,
                unitId: null);
            await act.Should().ThrowAsync<ArgumentException>();

            var common = await service.CreateAsync(
                OperatingCostCategory.CommonUpkeep,
                100m,
                DateTime.UtcNow,
                unitId: null,
                notes: "Hall lights");
            common.UnitId.Should().BeNull();
            common.Category.Should().Be(OperatingCostCategory.CommonUpkeep);

            var reno = () => service.CreateAsync(
                OperatingCostCategory.Renovation,
                500m,
                DateTime.UtcNow,
                unitId: null);
            await reno.Should().ThrowAsync<ArgumentException>();
        }
    }

    [Fact]
    public async Task SumByUnitAsync_GroupsAmounts()
    {
        var (db, service) = Create();
        await using (db)
        {
            var unit = new Unit { Id = Guid.NewGuid(), Number = "8", SqFt = 500, Beds = 1, Baths = 1 };
            db.Units.Add(unit);
            await db.SaveChangesAsync();

            await service.CreateAsync(OperatingCostCategory.Repair, 50m, DateTime.UtcNow, unit.Id);
            await service.CreateAsync(OperatingCostCategory.Utility, 30m, DateTime.UtcNow, unit.Id);
            await service.CreateAsync(OperatingCostCategory.CommonUpkeep, 20m, DateTime.UtcNow);

            var sums = await service.SumByUnitAsync();
            sums.Should().ContainSingle(s => s.UnitId == unit.Id && s.Total == 80m);
            sums.Should().ContainSingle(s => s.UnitId == null && s.Total == 20m);
        }
    }
}
