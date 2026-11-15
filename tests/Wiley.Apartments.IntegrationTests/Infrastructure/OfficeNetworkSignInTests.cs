using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wiley.Apartments.IntegrationTests.Support;
using Wiley.Apartments.Web.Data;
using Wiley.Apartments.Web.Infrastructure;

namespace Wiley.Apartments.IntegrationTests.Infrastructure;

public sealed class OfficeNetworkSignInTests
{
    [Theory]
    [InlineData("/_framework/blazor.web.js", true)]
    [InlineData("/health", true)]
    [InlineData("/api/exports/shared-contracts/operational-events", true)]
    [InlineData("/css/app.css", true)]
    [InlineData("/", false)]
    [InlineData("/Account/Login", false)]
    public void IsPassThrough_SkipsStaticAndApiPaths(string path, bool expected)
    {
        OfficeNetworkSignInMiddleware.IsPassThrough(path).Should().Be(expected);
    }

    [Fact]
    public async Task GetRoot_DoesNotSignInALocalIdentityUser()
    {
        await using var factory = new OfficeSignInFactory();
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var created = await users.CreateAsync(
                new ApplicationUser
                {
                    UserName = "clerk@townofwiley.gov",
                    Email = "clerk@townofwiley.gov",
                    EmailConfirmed = true,
                    DisplayName = "Town Clerk"
                },
                "Password1!");
            created.Succeeded.Should().BeTrue(string.Join(", ", created.Errors.Select(error => error.Description)));
        }

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.OriginalString.Should().Contain("/Account/Login");
        if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            string.Join(";", cookies).Should().NotContain(".AspNetCore.Identity.Application");
        }
    }

    private sealed class OfficeSignInFactory : ClerkSuiteWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ClerkSuite:OfficeNetworkSignIn"] = "true"
                });
            });
        }
    }
}
