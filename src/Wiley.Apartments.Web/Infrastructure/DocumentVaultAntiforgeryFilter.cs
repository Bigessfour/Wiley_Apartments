using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Wiley.Apartments.Web.Infrastructure;

/// <summary>
/// CSRF protection for cookie-authenticated vault endpoints (T030).
/// Accepts the standard antiforgery header from SfFileManager OnSend.
/// </summary>
public sealed class DocumentVaultAntiforgeryFilter(
    IAntiforgery antiforgery,
    IHostEnvironment environment,
    ILogger<DocumentVaultAntiforgeryFilter> logger) : IAsyncActionFilter
{
    public const string HeaderName = "RequestVerificationToken";

    private readonly IAntiforgery _antiforgery = antiforgery;
    private readonly IHostEnvironment _environment = environment;
    private readonly ILogger<DocumentVaultAntiforgeryFilter> _logger = logger;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        if (HttpMethods.IsGet(http.Request.Method) || HttpMethods.IsHead(http.Request.Method))
        {
            await next();
            return;
        }

        // Integration/E2E test hosts skip browser antiforgery wiring.
        if (_environment.IsEnvironment("Testing"))
        {
            await next();
            return;
        }

        try
        {
            await _antiforgery.ValidateRequestAsync(http);
            await next();
        }
        catch (AntiforgeryValidationException ex)
        {
            _logger.LogWarning(ex, "Document vault antiforgery validation failed.");
            context.Result = new BadRequestObjectResult(new
            {
                error = "Antiforgery token missing or invalid. Reload the page and try again."
            });
        }
    }
}
