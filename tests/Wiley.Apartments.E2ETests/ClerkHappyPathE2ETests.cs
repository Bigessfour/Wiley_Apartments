using Microsoft.Playwright;

namespace Wiley.Apartments.E2ETests;

/// <summary>
/// Authenticated clerk smoke. Development host seeds clerk@dev.local.
/// The staff page has no password form; the test uses the development sign-in endpoint.
/// </summary>
[Collection("E2E")]
public class ClerkHappyPathE2ETests(E2EWebApplicationFactory factory) : IAsyncLifetime
{
    private static readonly string[] HomeMarkers = ["Occupancy", "ClerkSuite", "Dashboard", "Collected"];
    private static readonly string[] UnitsMarkers = ["Units", "Town of Wiley", "Unit #"];
    private static readonly string[] PaymentsMarkers = ["ledger", "Record payment", "Post charge", "Tenant ledger"];
    private static readonly string[] MaintenanceMarkers = ["Maintenance", "work order", "New work order"];
    private static readonly string[] ScheduleMarkers = ["calendar", "Operations", "Schedule"];
    private static readonly string[] ReportsMarkers = ["Rent roll", "Reports", "Delinquency"];
    private static readonly string[] RentRollMarkers = ["Rent roll", "Print"];
    private static readonly string[] DocumentsMarkers = ["Document", "vault", "File"];

    private readonly E2EWebApplicationFactory _factory = factory;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

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
        page.SetDefaultTimeout(45_000);

        await SignInAsDevClerkAsync(page);

        await GotoAuthenticatedAsync(page, "/", HomeMarkers);
        await GotoAuthenticatedAsync(page, "/units", UnitsMarkers);
        await GotoAuthenticatedAsync(page, "/payments", PaymentsMarkers);
        await GotoAuthenticatedAsync(page, "/maintenance", MaintenanceMarkers);
        await GotoAuthenticatedAsync(page, "/schedule", ScheduleMarkers);
        await GotoAuthenticatedAsync(page, "/reports", ReportsMarkers);
        await GotoAuthenticatedAsync(page, "/reports/rent-roll", RentRollMarkers);
        await GotoAuthenticatedAsync(page, "/documents", DocumentsMarkers);
    }

    [Fact]
    public async Task LoginPage_HasNoPasswordForm()
    {
        var page = await _browser!.NewPageAsync();
        page.SetDefaultTimeout(20_000);

        await page.GotoAsync($"{_factory.E2EBaseUrl}/Account/Login");

        page.Url.Should().Contain("/Account/Login");
        (await page.Locator("input[type='password']").CountAsync()).Should().Be(0);
        var body = await page.ContentAsync();
        body.Should().Contain("Sign in on ClerkSuite");
        body.Should().NotContain("Invalid email or password");
    }

    private async Task SignInAsDevClerkAsync(IPage page)
    {
        var response = await page.Context.APIRequest.PostAsync(
            $"{_factory.E2EBaseUrl}/authentication/dev-sign-in");
        response.Ok.Should().BeTrue(await response.TextAsync());
    }

    private async Task GotoAuthenticatedAsync(IPage page, string path, string[] anyOfMarkers)
    {
        await page.GotoAsync($"{_factory.E2EBaseUrl}{path}", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });
        page.Url.Should().NotContain("/Account/Login");

        // Interactive Server pages paint after the circuit attaches — wait for any marker text.
        var deadline = DateTime.UtcNow.AddSeconds(30);
        string? lastBody = null;
        while (DateTime.UtcNow < deadline)
        {
            lastBody = await page.ContentAsync();
            if (lastBody.Contains("Sign in on ClerkSuite", StringComparison.OrdinalIgnoreCase)
                && lastBody.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Lost auth session mid happy-path.");
            }

            if (anyOfMarkers.Any(m => lastBody.Contains(m, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            // Also try visible text via Playwright (handles delayed DOM)
            foreach (var marker in anyOfMarkers)
            {
                var loc = page.GetByText(marker, new PageGetByTextOptions { Exact = false });
                if (await loc.CountAsync() > 0)
                {
                    try
                    {
                        await loc.First.WaitForAsync(new LocatorWaitForOptions
                        {
                            State = WaitForSelectorState.Visible,
                            Timeout = 2_000
                        });
                        return;
                    }
                    catch (TimeoutException)
                    {
                        // keep polling
                    }
                }
            }

            await page.WaitForTimeoutAsync(400);
        }

        var snippet = lastBody is null
            ? "(empty)"
            : lastBody.Length <= 500 ? lastBody : lastBody[^500..];
        throw new TimeoutException(
            $"Timed out waiting for markers [{string.Join(", ", anyOfMarkers)}] on {path}. Tail: {snippet}");
    }
}
