using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Wiley.Apartments.Web.Configuration;

namespace Wiley.Apartments.Web.Infrastructure;

/// <summary>
/// Silent ClerkSuite session. The hosted-UI domain comes from configuration.
/// An empty domain skips the probe so local tests keep the sign-in message.
/// </summary>
public static class CognitoStaffAuth
{
    public const string DefaultClientId = "258binbcvsms0rfqj5g20qakki";
    public const string CheckedCookie = "clerksuite-cognito-checked";
    public const string PkceCookie = "clerksuite-pkce";
    public const string ReturnCookie = "clerksuite-auth-return";

    public static bool ShouldProbe(HttpContext http)
    {
        if (!HttpMethods.IsGet(http.Request.Method))
        {
            return false;
        }

        if (http.User.Identity?.IsAuthenticated == true)
        {
            return false;
        }

        if (http.Request.Cookies.ContainsKey(CheckedCookie))
        {
            return false;
        }

        var options = http.RequestServices.GetService<IOptions<ClerkSuiteOptions>>()?.Value;
        if (string.IsNullOrWhiteSpace(options?.CognitoDomain))
        {
            return false;
        }

        return IsDocumentNavigation(http.Request.Path.Value);
    }

    public static bool IsDocumentNavigation(string? path)
    {
        if (string.IsNullOrEmpty(path) || path == "/")
        {
            return path == "/";
        }

        if (path.StartsWith("/authentication", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/Account", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/_", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
            || path.Contains('.', StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Same-origin path only. Drops absolute URLs and any return value that carries an email.
    /// </summary>
    public static string SafeReturnPath(string? value)
    {
        var path = LoginRedirectHelper.GetSafeReturnUrl(value);
        if (path.Contains('@', StringComparison.Ordinal)
            || path.Contains("email=", StringComparison.OrdinalIgnoreCase)
            || path.Contains("token=", StringComparison.OrdinalIgnoreCase)
            || path.Contains("access_token", StringComparison.OrdinalIgnoreCase))
        {
            return "/";
        }

        return path;
    }

    public static string? HostedAuthorizeUrl(
        string? domain,
        string clientId,
        string redirectUri,
        string returnPath,
        string codeChallenge,
        string state,
        bool promptLogin)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            return null;
        }

        var host = domain.Contains("://", StringComparison.Ordinal)
            ? domain.TrimEnd('/')
            : "https://" + domain.Trim().TrimEnd('/');
        var prompt = promptLogin ? "login" : "none";
        return host
            + "/oauth2/authorize?response_type=code"
            + "&client_id=" + Uri.EscapeDataString(clientId)
            + "&redirect_uri=" + Uri.EscapeDataString(redirectUri)
            + "&scope=" + Uri.EscapeDataString("openid email")
            + "&prompt=" + prompt
            + "&code_challenge_method=S256"
            + "&code_challenge=" + Uri.EscapeDataString(codeChallenge)
            + "&state=" + Uri.EscapeDataString(state);
    }

    public static string? BuildLogoutUrl(string? domain, string clientId, string logoutUri)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            return null;
        }

        var host = domain.Contains("://", StringComparison.Ordinal)
            ? domain.TrimEnd('/')
            : "https://" + domain.Trim().TrimEnd('/');
        return host
            + "/logout?client_id=" + Uri.EscapeDataString(clientId)
            + "&logout_uri=" + Uri.EscapeDataString(logoutUri);
    }

    public static string RedirectUri(HttpRequest request) =>
        $"{request.Scheme}://{request.Host}/authentication/login-callback";

    public static string CreateCodeVerifier()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Base64Url(bytes);
    }

    public static string CreateCodeChallenge(string verifier)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        return Base64Url(hash);
    }

    public static string CreateState() => Base64Url(RandomNumberGenerator.GetBytes(16));

    public static bool TryReadIdentity(
        string? idToken,
        string clientId,
        out string sub,
        out string? email,
        out IReadOnlyList<string> groups)
    {
        sub = "";
        email = null;
        groups = [];
        if (string.IsNullOrWhiteSpace(idToken))
        {
            return false;
        }

        var parts = idToken.Split('.');
        if (parts.Length < 2)
        {
            return false;
        }

        try
        {
            var json = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!AudienceMatches(root, clientId))
            {
                return false;
            }

            if (!root.TryGetProperty("sub", out var subEl) || subEl.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            if (!Guid.TryParse(subEl.GetString(), out var subGuid))
            {
                return false;
            }

            sub = subGuid.ToString();
            if (root.TryGetProperty("email", out var emailEl) && emailEl.ValueKind == JsonValueKind.String)
            {
                email = emailEl.GetString();
            }

            if (root.TryGetProperty("cognito:groups", out var groupEl) && groupEl.ValueKind == JsonValueKind.Array)
            {
                groups = groupEl.EnumerateArray()
                    .Where(item => item.ValueKind == JsonValueKind.String)
                    .Select(item => item.GetString())
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .Cast<string>()
                    .ToArray();
            }

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static async Task SignInAsync(HttpContext http, string sub, string? email, IReadOnlyList<string> groups)
    {
        var name = string.IsNullOrWhiteSpace(email) ? sub : email;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, sub),
            new(ClaimTypes.Name, name!),
            new("sub", sub)
        };
        if (!string.IsNullOrWhiteSpace(email))
        {
            claims.Add(new Claim(ClaimTypes.Email, email));
        }

        foreach (var group in groups)
        {
            claims.Add(new Claim(ClaimTypes.Role, group));
        }

        var identity = new ClaimsIdentity(
            claims,
            IdentityConstants.ApplicationScheme,
            ClaimTypes.Name,
            ClaimTypes.Role);
        await http.SignInAsync(IdentityConstants.ApplicationScheme, new ClaimsPrincipal(identity));
    }

    public static async Task<string?> ExchangeCodeAsync(
        HttpClient http,
        string domain,
        string clientId,
        string code,
        string verifier,
        string redirectUri,
        CancellationToken cancellationToken)
    {
        var host = domain.Contains("://", StringComparison.Ordinal)
            ? domain.TrimEnd('/')
            : "https://" + domain.Trim().TrimEnd('/');
        using var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = clientId,
            ["code"] = code,
            ["code_verifier"] = verifier,
            ["redirect_uri"] = redirectUri
        });
        using var response = await http.PostAsync(host + "/oauth2/token", body, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return doc.RootElement.TryGetProperty("id_token", out var token) ? token.GetString() : null;
    }

    private static bool AudienceMatches(JsonElement root, string clientId)
    {
        if (!root.TryGetProperty("aud", out var aud))
        {
            return false;
        }

        if (aud.ValueKind == JsonValueKind.String)
        {
            return string.Equals(aud.GetString(), clientId, StringComparison.Ordinal);
        }

        if (aud.ValueKind == JsonValueKind.Array)
        {
            return aud.EnumerateArray().Any(item =>
                item.ValueKind == JsonValueKind.String
                && string.Equals(item.GetString(), clientId, StringComparison.Ordinal));
        }

        return false;
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }

        return Convert.FromBase64String(padded);
    }
}
