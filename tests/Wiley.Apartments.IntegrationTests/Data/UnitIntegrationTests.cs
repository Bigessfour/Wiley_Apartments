using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wiley.Apartments.Contracts;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.IntegrationTests.Data;

public class UnitSeederIntegrationTests(Support.ClerkSuiteWebApplicationFactory factory) : IClassFixture<Support.ClerkSuiteWebApplicationFactory>
{
    private readonly Support.ClerkSuiteWebApplicationFactory _factory = factory;

    [Fact]
    public async Task UnitSeeder_SeedsSixteenResidentialPlusCommunityCenter()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApartmentsDbContext>();
        var seeder = scope.ServiceProvider.GetRequiredService<IUnitSeeder>();

        db.Units.RemoveRange(db.Units);
        await db.SaveChangesAsync();

        await seeder.SeedAsync();

        var units = await db.Units.OrderBy(u => u.Number).ToListAsync();
        var residential = units.Where(u => !u.IsFacility).ToList();
        var facilities = units.Where(u => u.IsFacility).ToList();

        residential.Should().HaveCount(16);
        residential.Select(u => u.Number).Should().BeEquivalentTo(Enumerable.Range(1, 16).Select(i => i.ToString()));
        residential.Should().OnlyContain(u => u.Status == UnitStatus.Vacant);

        facilities.Should().ContainSingle();
        facilities[0].Number.Should().Be("CC");
        facilities[0].IsFacility.Should().BeTrue();
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
