using System.Security.Cryptography;
using System.Text;

namespace Wiley.Apartments.Web.Infrastructure;

/// <summary>
/// Operating-cost export is for a signed-in clerk or the NAS push script.
/// The header is ignored when no key is configured. The key is never logged.
/// </summary>
public static class ExportAccess
{
    public const string HeaderName = "X-ClerkSuite-Export-Key";

    public static bool IsAllowed(HttpContext http, string? configuredKey)
    {
        if (http.User.Identity?.IsAuthenticated == true)
        {
            return true;
        }

        if (string.IsNullOrEmpty(configuredKey))
        {
            return false;
        }

        if (!http.Request.Headers.TryGetValue(HeaderName, out var provided))
        {
            return false;
        }

        var given = provided.ToString();
        if (given.Length == 0)
        {
            return false;
        }

        var givenHash = SHA256.HashData(Encoding.UTF8.GetBytes(given));
        var configuredHash = SHA256.HashData(Encoding.UTF8.GetBytes(configuredKey));
        return CryptographicOperations.FixedTimeEquals(givenHash, configuredHash);
    }
}
