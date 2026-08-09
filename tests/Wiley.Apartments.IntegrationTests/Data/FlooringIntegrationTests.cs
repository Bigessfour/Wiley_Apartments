using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wiley.Apartments.Contracts;
using Wiley.Apartments.Domain;
using Wiley.Apartments.IntegrationTests.Support;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.IntegrationTests.Data;

public class FlooringIntegrationTests : IClassFixture<ClerkSuiteWebApplicationFactory>
{
    private readonly ClerkSuiteWebApplicationFactory _factory;

    public FlooringIntegrationTests(ClerkSuiteWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FlooringService_CreatesFlooringForUnit()
    {
        using var scope = _factory.Services.CreateScope();
        var unitService = scope.ServiceProvider.GetRequiredService<IUnitService>();
        var flooringService = scope.ServiceProvider.GetRequiredService<IFlooringService>();
        var db = scope.ServiceProvider.GetRequiredService<ApartmentsDbContext>();

        db.Floorings.RemoveRange(db.Floorings);
        db.Units.RemoveRange(db.Units);
        await db.SaveChangesAsync();

        var unit = await unitService.CreateAsync(new Unit
        {
            Number = "77",
            SqFt = 720,
            Beds = 2,
            Baths = 1,
            Status = UnitStatus.Vacant
        });

        var flooring = await flooringService.CreateAsync(new Flooring
        {
            UnitId = unit.Id,
            Type = "Luxury vinyl plank",
            InstallDate = new DateOnly(2024, 2, 1),
            Condition = "Good",
            Notes = "Hallway and kitchen"
        });

        flooring.Id.Should().NotBeEmpty();
        (await flooringService.GetByUnitIdAsync(unit.Id)).Should().ContainSingle();
    }
}
