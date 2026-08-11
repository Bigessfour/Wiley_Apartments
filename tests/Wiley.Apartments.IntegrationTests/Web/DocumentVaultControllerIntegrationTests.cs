using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Wiley.Apartments.IntegrationTests.Support;

namespace Wiley.Apartments.IntegrationTests.Web;

public sealed class DocumentVaultControllerIntegrationTests(ClerkSuiteWebApplicationFactory factory) : IClassFixture<ClerkSuiteWebApplicationFactory>
{
    private readonly ClerkSuiteWebApplicationFactory _factory = factory;

    [Fact]
    public async Task FileOperations_Unauthenticated_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.PostAsJsonAsync(
            "/api/document-vault/FileOperations",
            new { action = "read", path = "/", showHiddenItems = false });

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task Health_ReturnsSuccess()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
