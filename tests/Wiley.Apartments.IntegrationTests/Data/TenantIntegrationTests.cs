using Microsoft.Extensions.DependencyInjection;
using Wiley.Apartments.Contracts;
using Wiley.Apartments.Domain;

namespace Wiley.Apartments.IntegrationTests.Data;

public class TenantIntegrationTests(Support.ClerkSuiteWebApplicationFactory factory) : IClassFixture<Support.ClerkSuiteWebApplicationFactory>
{
    private readonly Support.ClerkSuiteWebApplicationFactory _factory = factory;

    [Fact]
    public async Task TenantService_CreateSearchSoftDelete_RoundTrip()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ITenantService>();

        var created = await service.CreateAsync(new Tenant
        {
            FirstName = "Casey",
            LastName = "Rivera",
            Phone = "555-0100",
            Email = "casey@example.com",
            EmergencyContact = "Alex Rivera 555-0101"
        });

        var found = await service.SearchAsync("rivera");
        found.Should().Contain(t => t.Id == created.Id);

        await service.AddHouseholdMemberAsync(created.Id, new HouseholdMember
        {
            FullName = "Jordan Rivera",
            Relationship = "Spouse"
        });

        var detail = await service.GetByIdAsync(created.Id);
        detail!.HouseholdMembers.Should().ContainSingle(m => m.FullName == "Jordan Rivera");

        await service.SoftDeleteAsync(created.Id);
        (await service.SearchAsync()).Should().NotContain(t => t.Id == created.Id);
        (await service.SearchAsync(includeDeleted: true)).Should().Contain(t => t.Id == created.Id);
    }
}
