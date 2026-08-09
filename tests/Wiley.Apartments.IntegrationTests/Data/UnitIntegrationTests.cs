using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wiley.Apartments.Contracts;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.IntegrationTests.Data;

public class UnitSeederIntegrationTests : IClassFixture<Support.ClerkSuiteWebApplicationFactory>
{
    private readonly Support.ClerkSuiteWebApplicationFactory _factory;

    public UnitSeederIntegrationTests(Support.ClerkSuiteWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UnitSeeder_SeedsSixteenPlaceholderUnits()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApartmentsDbContext>();
        var seeder = scope.ServiceProvider.GetRequiredService<IUnitSeeder>();

        db.Units.RemoveRange(db.Units);
        await db.SaveChangesAsync();

        await seeder.SeedAsync();

        var units = await db.Units.OrderBy(u => u.Number).ToListAsync();
        units.Should().HaveCount(16);
        units.Select(u => u.Number).Should().BeEquivalentTo(Enumerable.Range(1, 16).Select(i => i.ToString()));
        units.Should().OnlyContain(u => u.Status == UnitStatus.Vacant);
    }

    [Fact]
    public async Task UnitService_UpdatesSeededUnit()
    {
        using var scope = _factory.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<IUnitSeeder>();
        var service = scope.ServiceProvider.GetRequiredService<IUnitService>();

        await seeder.SeedAsync();
        var unit = (await service.GetAllAsync()).First(u => u.Number == "1");

        unit.Status = UnitStatus.MakeReady;
        unit.SqFt = 810;
        unit.Notes = "Integration test update";
        var updated = await service.UpdateAsync(unit);

        updated.Status.Should().Be(UnitStatus.MakeReady);
        updated.SqFt.Should().Be(810);
        updated.Notes.Should().Be("Integration test update");
    }
}
