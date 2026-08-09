using System.Net;
using Microsoft.Playwright;

namespace Wiley.Apartments.E2ETests;

[Collection("E2E")]
public class UnitsPageE2ETests : IAsyncLifetime
{
    private readonly E2EWebApplicationFactory _factory;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public UnitsPageE2ETests(E2EWebApplicationFactory factory)
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
    public async Task UnitsPage_RedirectsToLogin_WhenUnauthenticated()
    {
        using var client = new HttpClient { BaseAddress = new Uri(_factory.E2EBaseUrl) };
        var response = await client.GetAsync("/units");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("/Account/Login");
    }

    [Fact]
    public async Task UnitsPage_RedirectsToLogin_Browser()
    {
        var page = await _browser!.NewPageAsync();
        await page.GotoAsync($"{_factory.E2EBaseUrl}/units");

        page.Url.Should().Contain("/Account/Login");
    }
}
