using System.Net;
using Wiley.Apartments.IntegrationTests.Support;

namespace Wiley.Apartments.IntegrationTests.Web;

public class AuthFlowIntegrationTests : IClassFixture<ClerkSuiteWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthFlowIntegrationTests(ClerkSuiteWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task LoginPage_ReturnsOk()
    {
        var response = await _client.GetAsync("/Account/Login");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("ClerkSuite");
        body.Should().Contain("Syncfusion");
    }

    [Fact]
    public async Task Home_RequiresAuthentication()
    {
        var response = await _client.GetAsync("/");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found);
        response.Headers.Location!.OriginalString.Should().Contain("/Account/Login");
    }
}
