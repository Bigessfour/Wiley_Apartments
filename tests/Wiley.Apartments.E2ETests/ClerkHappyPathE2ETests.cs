using Microsoft.Playwright;

namespace Wiley.Apartments.E2ETests;

/// <summary>
/// Authenticated clerk smoke: login (dev seed) then land on daily surfaces.
/// Development host seeds <c>clerk@dev.local</c> / <c>Password1!</c> via IdentitySeeder.
/// </summary>
[Collection("E2E")]
public class ClerkHappyPathE2ETests : IAsyncLifetime
{
    public const string DevClerkEmail = "clerk@dev.local";
    public const string DevClerkPassword = "Password1!";

    private readonly E2EWebApplicationFactory _factory;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public ClerkHappyPathE2ETests(E2EWebApplicationFactory factory)
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
    public async Task Clerk_CanSignIn_AndOpenDailySurfaces()
    {
        var page = await _browser!.NewPageAsync();
        page.SetDefaultTimeout(30_000);

        await SignInAsDevClerkAsync(page);

        // Dashboard (home)
        await page.GotoAsync($"{_factory.E2EBaseUrl}/");
        await page.WaitForURLAsync(url => !url.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase));
        var dash = await page.ContentAsync();
        var dashOk = dash.Contains("ClerkSuite", StringComparison.OrdinalIgnoreCase)
                     || dash.Contains("Occupancy", StringComparison.OrdinalIgnoreCase)
                     || dash.Contains("Dashboard", StringComparison.OrdinalIgnoreCase);
        dashOk.Should().BeTrue("dashboard should render clerk chrome after login");
        page.Url.Should().NotContain("/Account/Login");

        // Units
        await page.GotoAsync($"{_factory.E2EBaseUrl}/units");
        await ExpectAuthenticatedSurfaceAsync(page, mustContain: "Units");

        // Payments / ledger
        await page.GotoAsync($"{_factory.E2EBaseUrl}/payments");
        await ExpectAuthenticatedSurfaceAsync(page, mustContain: "ledger", alternate: "Payment");

        // Maintenance
        await page.GotoAsync($"{_factory.E2EBaseUrl}/maintenance");
        await ExpectAuthenticatedSurfaceAsync(page, mustContain: "Maintenance", alternate: "work order");

        // Schedule
        await page.GotoAsync($"{_factory.E2EBaseUrl}/schedule");
        await ExpectAuthenticatedSurfaceAsync(page, mustContain: "calendar", alternate: "Schedule");

        // Reports hub
        await page.GotoAsync($"{_factory.E2EBaseUrl}/reports");
        await ExpectAuthenticatedSurfaceAsync(page, mustContain: "Rent roll", alternate: "Reports");

        // Rent roll print surface
        await page.GotoAsync($"{_factory.E2EBaseUrl}/reports/rent-roll");
        await ExpectAuthenticatedSurfaceAsync(page, mustContain: "Rent roll", alternate: "rent");

        // Documents vault chrome
        await page.GotoAsync($"{_factory.E2EBaseUrl}/documents");
        await ExpectAuthenticatedSurfaceAsync(page, mustContain: "Document", alternate: "vault");
    }

    [Fact]
    public async Task Clerk_InvalidPassword_ShowsError_StaysOnLogin()
    {
        var page = await _browser!.NewPageAsync();
        page.SetDefaultTimeout(20_000);

        await page.GotoAsync($"{_factory.E2EBaseUrl}/Account/Login");
        await page.Locator("input[autocomplete='username'], input[name='Input.Email']").First.FillAsync(DevClerkEmail);
        await page.Locator("input[type='password'], input[autocomplete='current-password']").First.FillAsync("WrongPassword1!");
        await page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();
        await page.WaitForTimeoutAsync(1500);

        page.Url.Should().Contain("/Account/Login");
        var body = await page.ContentAsync();
        body.Should().Contain("Invalid email or password");
    }

    private async Task SignInAsDevClerkAsync(IPage page)
    {
        await page.GotoAsync($"{_factory.E2EBaseUrl}/Account/Login");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Blazor Identity SSR form — bind attributes may render as name="Input.Email"
        var email = page.Locator("input[autocomplete='username'], input[name='Input.Email'], input[type='email']").First;
        var password = page.Locator("input[autocomplete='current-password'], input[type='password']").First;

        await email.FillAsync(DevClerkEmail);
        await password.FillAsync(DevClerkPassword);
        await page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();

        // forceLoad redirect after cookie sign-in
        await page.WaitForURLAsync(
            url => !url.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase),
            new PageWaitForURLOptions { Timeout = 30_000 });
    }

    private static async Task ExpectAuthenticatedSurfaceAsync(IPage page, string mustContain, string? alternate = null)
    {
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        page.Url.Should().NotContain("/Account/Login");
        var body = await page.ContentAsync();
        body.Should().NotContain("Invalid email or password");
        var ok = body.Contains(mustContain, StringComparison.OrdinalIgnoreCase)
                 || (alternate is not null && body.Contains(alternate, StringComparison.OrdinalIgnoreCase));
        ok.Should().BeTrue($"expected page to mention '{mustContain}'" + (alternate is null ? "" : $" or '{alternate}'"));
    }
}
