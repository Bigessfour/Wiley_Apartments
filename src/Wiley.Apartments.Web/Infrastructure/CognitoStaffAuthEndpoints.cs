using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Wiley.Apartments.Web.Configuration;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.Web.Infrastructure;

public static class CognitoStaffAuthEndpoints
{
    public static WebApplication MapCognitoStaffAuth(this WebApplication app)
    {
        app.MapGet("/authentication/cognito-start", (HttpContext http, string? returnPath) =>
            Begin(http, returnPath, promptLogin: false));

        app.MapGet("/authentication/cognito-login", (HttpContext http, string? returnPath) =>
            Begin(http, returnPath, promptLogin: true));

        app.MapGet("/authentication/login-callback", async (
            HttpContext http,
            IOptions<ClerkSuiteOptions> options,
            string? code,
            string? state,
            string? error,
            CancellationToken cancellationToken) =>
        {
            var returnPath = CognitoStaffAuth.SafeReturnPath(http.Request.Cookies[CognitoStaffAuth.ReturnCookie]);
            http.Response.Cookies.Delete(CognitoStaffAuth.PkceCookie);
            http.Response.Cookies.Delete(CognitoStaffAuth.ReturnCookie);

            var verifier = http.Request.Cookies[CognitoStaffAuth.PkceCookie];
            var expectedState = http.Request.Cookies["clerksuite-auth-state"];
            http.Response.Cookies.Delete("clerksuite-auth-state");

            var domain = options.Value.CognitoDomain;
            if (!string.IsNullOrEmpty(error)
                || string.IsNullOrEmpty(code)
                || string.IsNullOrEmpty(verifier)
                || string.IsNullOrEmpty(domain)
                || !string.Equals(state, expectedState, StringComparison.Ordinal))
            {
                MarkChecked(http);
                return Results.Redirect(returnPath);
            }

            using var client = new HttpClient();
            var idToken = await CognitoStaffAuth.ExchangeCodeAsync(
                client,
                domain,
                ClientId(options.Value),
                code,
                verifier,
                CognitoStaffAuth.RedirectUri(http.Request),
                cancellationToken);
            if (CognitoStaffAuth.TryReadIdentity(idToken, ClientId(options.Value), out var sub, out var email, out var groups))
            {
                await CognitoStaffAuth.SignInAsync(http, sub, email, groups);
            }

            return Results.Redirect(returnPath);
        });

        if (app.Environment.IsDevelopment())
        {
            app.MapPost("/authentication/dev-sign-in", async (
                    SignInManager<ApplicationUser> signIn,
                    UserManager<ApplicationUser> users) =>
                {
                    var user = await users.FindByEmailAsync("clerk@dev.local");
                    if (user is null)
                    {
                        return Results.NotFound();
                    }

                    await signIn.SignInAsync(user, isPersistent: true);
                    return Results.Ok();
                })
                .DisableAntiforgery();
        }

        return app;
    }

    private static IResult Begin(HttpContext http, string? returnPath, bool promptLogin)
    {
        var options = http.RequestServices.GetRequiredService<IOptions<ClerkSuiteOptions>>().Value;
        var safe = CognitoStaffAuth.SafeReturnPath(returnPath);
        if (string.IsNullOrWhiteSpace(options.CognitoDomain))
        {
            MarkChecked(http);
            return Results.Redirect("/Account/Login?ReturnUrl=" + Uri.EscapeDataString(safe));
        }

        var verifier = CognitoStaffAuth.CreateCodeVerifier();
        var state = CognitoStaffAuth.CreateState();
        WriteCookie(http, CognitoStaffAuth.PkceCookie, verifier);
        WriteCookie(http, CognitoStaffAuth.ReturnCookie, safe);
        WriteCookie(http, "clerksuite-auth-state", state);
        var url = CognitoStaffAuth.HostedAuthorizeUrl(
            options.CognitoDomain,
            ClientId(options),
            CognitoStaffAuth.RedirectUri(http.Request),
            safe,
            CognitoStaffAuth.CreateCodeChallenge(verifier),
            state,
            promptLogin);
        return Results.Redirect(url!);
    }

    private static void MarkChecked(HttpContext http) =>
        WriteCookie(http, CognitoStaffAuth.CheckedCookie, "1");

    private static void WriteCookie(HttpContext http, string name, string value)
    {
        http.Response.Cookies.Append(name, value, new CookieOptions
        {
            HttpOnly = true,
            Secure = http.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            MaxAge = TimeSpan.FromMinutes(10)
        });
    }

    private static string ClientId(ClerkSuiteOptions options) =>
        string.IsNullOrWhiteSpace(options.CognitoClientId)
            ? CognitoStaffAuth.DefaultClientId
            : options.CognitoClientId;
}
