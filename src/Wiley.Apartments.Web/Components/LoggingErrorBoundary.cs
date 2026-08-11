using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Wiley.Apartments.Web.Components;

/// <summary>
/// Error boundary that logs the exception before showing a recovery UI.
/// </summary>
public sealed class LoggingErrorBoundary : ErrorBoundary
{
    [Inject]
    private ILogger<LoggingErrorBoundary> Logger { get; set; } = default!;

    [Parameter]
    public string Region { get; set; } = "page";

    protected override Task OnErrorAsync(Exception exception)
    {
        Logger.LogError(
            exception,
            "Unhandled Blazor UI exception in region {Region}",
            Region);
        return Task.CompletedTask;
    }
}
