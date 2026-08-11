using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wiley.Apartments.Contracts;
using Wiley.Apartments.Domain;
using Wiley.Apartments.IntegrationTests.Support;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.IntegrationTests.Data;

public class AssetIntegrationTests(ClerkSuiteWebApplicationFactory factory) : IClassFixture<ClerkSuiteWebApplicationFactory>
{
    private readonly ClerkSuiteWebApplicationFactory _factory = factory;

    [Fact]
    public async Task AssetService_CreatesAssetForUnit()
    {
        using var scope = _factory.Services.CreateScope();
        var unitService = scope.ServiceProvider.GetRequiredService<IUnitService>();
        var assetService = scope.ServiceProvider.GetRequiredService<IAssetService>();
        var db = scope.ServiceProvider.GetRequiredService<ApartmentsDbContext>();

        db.Units.RemoveRange(db.Units);
        db.Assets.RemoveRange(db.Assets);
        await db.SaveChangesAsync();

        var unit = await unitService.CreateAsync(new Unit
        {
            Number = "88",
            SqFt = 700,
            Beds = 2,
            Baths = 1,
            Status = UnitStatus.Vacant
        });

        var asset = await assetService.CreateAsync(new Asset
        {
            UnitId = unit.Id,
            Type = "Water heater",
            Serial = "WH-88-001",
            Make = "Rheem",
            Model = "XR90",
            WarrantyStart = new DateOnly(2025, 6, 1),
            WarrantyEnd = new DateOnly(2030, 6, 1)
        });

        asset.Id.Should().NotBeEmpty();
        (await assetService.GetByUnitIdAsync(unit.Id)).Should().ContainSingle();
        (await assetService.SearchBySerialAsync("WH-88")).Should().ContainSingle();
    }
}
