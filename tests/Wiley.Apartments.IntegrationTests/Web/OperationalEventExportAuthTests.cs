using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Wiley.Apartments.IntegrationTests.Support;
using Wiley.Apartments.Web.Infrastructure;

namespace Wiley.Apartments.IntegrationTests.Web;

public class OperationalEventExportAuthTests
{
    [Fact]
    public async Task Export_WithoutCredential_IsUnauthorized()
    {
        await using var factory = new ClerkSuiteWebApplicationFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/api/exports/shared-contracts/operational-events");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Export_WithWrongKey_IsUnauthorized()
    {
        await using var factory = new ExportKeyFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add(ExportAccess.HeaderName, "not-the-key");

        var response = await client.GetAsync("/api/exports/shared-contracts/operational-events");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Export_WithMatchingKey_IsOk()
    {
        await using var factory = new ExportKeyFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add(ExportAccess.HeaderName, ExportKeyFactory.Key);

        var response = await client.GetAsync("/api/exports/shared-contracts/operational-events?period=2026-10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed class ExportKeyFactory : ClerkSuiteWebApplicationFactory
    {
        public const string Key = "test-export-key";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ClerkSuite:ExportKey"] = Key
                });
            });
        }
    }
}
