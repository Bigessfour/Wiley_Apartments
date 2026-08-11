using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wiley.Apartments.IntegrationTests.Support;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.IntegrationTests.Web;

public class DatabaseIntegrationTests(ClerkSuiteWebApplicationFactory factory) : IClassFixture<ClerkSuiteWebApplicationFactory>
{
    private readonly ClerkSuiteWebApplicationFactory _factory = factory;

    [Fact]
    public async Task Database_CreatesIdentityAndAuditTables()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApartmentsDbContext>();

        var canConnect = await db.Database.CanConnectAsync();
        canConnect.Should().BeTrue();

        var auditTableExists = await db.AuditLogs.AnyAsync();
        auditTableExists.Should().BeFalse();
    }
}
