using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wiley.Apartments.IntegrationTests.Support;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.IntegrationTests.Data;

public class AuditLogIntegrationTests : IClassFixture<ClerkSuiteWebApplicationFactory>
{
    private readonly ClerkSuiteWebApplicationFactory _factory;

    public AuditLogIntegrationTests(ClerkSuiteWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SaveChanges_WritesAuditLog_ForMutations()
    {
        using var scope = _factory.Services.CreateScope();
        var accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = new DefaultHttpContext
        {
            User = new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(
                    [new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "clerk@town.gov")],
                    "Test"))
        };

        var db = scope.ServiceProvider.GetRequiredService<ApartmentsDbContext>();
        var interceptor = scope.ServiceProvider.GetRequiredService<AuditSaveChangesInterceptor>();

        var connection = db.Database.GetDbConnection();
        var options = new DbContextOptionsBuilder<ApartmentsDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(interceptor)
            .Options;

        await using var auditedContext = new ApartmentsDbContext(options);
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        auditedContext.Users.Add(new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "audit-test@town.gov",
            Email = "audit-test@town.gov",
            NormalizedUserName = "AUDIT-TEST@TOWN.GOV",
            NormalizedEmail = "AUDIT-TEST@TOWN.GOV",
            EmailConfirmed = true
        });

        await auditedContext.SaveChangesAsync();

        var auditRows = await db.AuditLogs
            .Where(a => a.UserId == "clerk@town.gov" && a.EntityType == nameof(ApplicationUser))
            .ToListAsync();

        auditRows.Should().NotBeEmpty();
        auditRows.Should().Contain(a => a.Action == "Create");
    }
}
