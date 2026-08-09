using System.Net;
using Microsoft.Playwright;

namespace Wiley.Apartments.E2ETests;

public class LoginE2ETests : IClassFixture<E2EWebApplicationFactory>, IAsyncLifetime
{
    private readonly E2EWebApplicationFactory _factory;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public LoginE2ETests(E2EWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
    }

    public async Task DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
    }

    [Fact]
    public async Task LoginPage_ShowsClerkSuiteBranding_Http()
    {
        using var client = new HttpClient { BaseAddress = new Uri(_factory.E2EBaseUrl) };
        var response = await client.GetAsync("/Account/Login");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("ClerkSuite");
    }

    [Fact]
    public async Task LoginPage_ShowsClerkSuiteBranding_Browser()
    {
        var page = await _browser!.NewPageAsync();
        await page.GotoAsync($"{_factory.E2EBaseUrl}/Account/Login");

        var content = await page.ContentAsync();
        content.Should().Contain("ClerkSuite");
        content.Should().Contain("Sign in");
    }

    [Fact]
    public async Task Root_RedirectsToLogin_WhenUnauthenticated()
    {
        var page = await _browser!.NewPageAsync();
        await page.GotoAsync($"{_factory.E2EBaseUrl}/");

        page.Url.Should().Contain("/Account/Login");
    }
}
