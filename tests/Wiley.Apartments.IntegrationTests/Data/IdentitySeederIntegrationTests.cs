using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Wiley.Apartments.IntegrationTests.Support;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.IntegrationTests.Data;

public class IdentitySeederIntegrationTests(ClerkSuiteWebApplicationFactory factory) : IClassFixture<ClerkSuiteWebApplicationFactory>
{
    private readonly ClerkSuiteWebApplicationFactory _factory = factory;

    [Fact]
    public async Task SeedAsync_CreatesConfiguredUser()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var options = Options.Create(new SeedUserOptions
        {
            Users =
            [
                new SeedUser
                {
                    Email = "integration@test.local",
                    Password = "Password1!",
                    DisplayName = "Integration User"
                }
            ]
        });

        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

        var seeder = new IdentitySeeder(
            userManager,
            options,
            environment,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<IdentitySeeder>.Instance);

        await seeder.SeedAsync();

        var user = await userManager.FindByEmailAsync("integration@test.local");
        user.Should().NotBeNull();
        user!.DisplayName.Should().Be("Integration User");
    }

    [Fact]
    public async Task SeedAsync_IsIdempotent()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var options = Options.Create(new SeedUserOptions
        {
            Users =
            [
                new SeedUser { Email = "idempotent@test.local", Password = "Password1!" }
            ]
        });

        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

        var seeder = new IdentitySeeder(
            userManager,
            options,
            environment,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<IdentitySeeder>.Instance);

        await seeder.SeedAsync();
        await seeder.SeedAsync();

        var users = userManager.Users.Where(u => u.Email == "idempotent@test.local").ToList();
        users.Should().HaveCount(1);
    }
}
