using Microsoft.Extensions.DependencyInjection;
using Wiley.Apartments.IntegrationTests.Support;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.IntegrationTests.Data;

public class PostgresIdentitySequenceRepairTests
{
    [Fact]
    public async Task Align_OnSqlite_DoesNotThrow()
    {
        await using var factory = new ClerkSuiteWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApartmentsDbContext>();

        var act = () => PostgresIdentitySequenceRepair.AlignAsync(db);

        await act.Should().NotThrowAsync();
    }
}
