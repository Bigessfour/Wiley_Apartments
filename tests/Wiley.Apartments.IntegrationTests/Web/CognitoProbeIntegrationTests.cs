using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Wiley.Apartments.IntegrationTests.Support;
using Wiley.Apartments.Web.Infrastructure;

namespace Wiley.Apartments.IntegrationTests.Web;

public class CognitoProbeIntegrationTests
{
    [Fact]
    public async Task DocumentGet_ProbesOnce_ThenStops()
    {
        await using var factory = new ProbeFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var first = await client.GetAsync("/");
        first.StatusCode.Should().Be(HttpStatusCode.Redirect);
        first.Headers.Location!.OriginalString.Should().StartWith("/authentication/cognito-start");

        var again = new HttpRequestMessage(HttpMethod.Get, "/");
        again.Headers.Add("Cookie", CognitoStaffAuth.CheckedCookie + "=1");
        var second = await client.SendAsync(again);
        second.StatusCode.Should().Be(HttpStatusCode.Redirect);
        second.Headers.Location!.OriginalString.Should().Contain("/Account/Login");
        second.Headers.Location!.OriginalString.Should().NotContain("/authentication/cognito-start");
    }

    [Fact]
    public async Task Login_WhenHostedUiIsConfigured_RedirectsToClerkSuiteLogin()
    {
        await using var factory = new ProbeFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/Account/Login?ReturnUrl=/units");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.OriginalString.Should().StartWith("/authentication/cognito-login?return=");
        response.Headers.Location!.OriginalString.Should().Contain("%2Funits");
    }

    private sealed class ProbeFactory : ClerkSuiteWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ClerkSuite:CognitoDomain"] = "tow-gov-staff.auth.us-east-2.amazoncognito.com"
                });
            });
        }
    }
}
