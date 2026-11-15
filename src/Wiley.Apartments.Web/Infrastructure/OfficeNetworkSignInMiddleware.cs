using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Wiley.Apartments.Web.Configuration;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.Web.Infrastructure;

/// <summary>
/// Signs in the office clerk on the tailnet so Town Hub can open ClerkSuite without a second form.
/// </summary>
public sealed class OfficeNetworkSignInMiddleware(RequestDelegate next, IOptions<ClerkSuiteOptions> options)
{
    public const string AttemptCookieName = "clerksuite-office-signin";

    private readonly RequestDelegate _next = next;
    private readonly ClerkSuiteOptions _options = options.Value;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.OfficeNetworkSignIn
            || context.User.Identity?.IsAuthenticated == true
            || IsPassThrough(context.Request.Path)
            || HttpMethods.IsPost(context.Request.Method)
            || context.Request.Cookies.ContainsKey(AttemptCookieName))
        {
            await _next(context);
            return;
        }

        var users = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var clerk = await FindOfficeClerkAsync(users, _options, context.RequestAborted);
        if (clerk is null)
        {
            await _next(context);
            return;
        }

        var signIn = context.RequestServices.GetRequiredService<SignInManager<ApplicationUser>>();
        await signIn.SignInAsync(clerk, isPersistent: true);

        context.Response.Cookies.Append(AttemptCookieName, "1", new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromMinutes(2),
            Path = "/"
        });

        var target = context.Request.Path.StartsWithSegments("/Account", StringComparison.OrdinalIgnoreCase)
            ? "/"
            : context.Request.Path + context.Request.QueryString;
        context.Response.Redirect(target);
    }

    public static bool IsPassThrough(PathString path)
    {
        var value = path.Value ?? string.Empty;
        if (value.StartsWith("/_", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("/api/", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("/css/", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("/js/", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("/lib/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return Path.HasExtension(value);
    }

    public static async Task<ApplicationUser?> FindOfficeClerkAsync(
        UserManager<ApplicationUser> users,
        ClerkSuiteOptions options,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(options.OfficeClerkEmail))
        {
            var named = await users.FindByEmailAsync(options.OfficeClerkEmail);
            if (named is not null)
            {
                return named;
            }
        }

        return await users.Users
            .OrderBy(user => user.Email)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
