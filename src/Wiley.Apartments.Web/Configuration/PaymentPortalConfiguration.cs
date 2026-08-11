namespace Wiley.Apartments.Web.Configuration;

/// <summary>Payment portal deep-link validation (spec edge case 4 — no silent fallback).</summary>
public static class PaymentPortalConfiguration
{
    public const string ItContactNote =
        "Contact town IT to set ClerkSuite__PaymentPortalUrl (or PaymentPortalUrl) to the town PayStar portal URL.";

    public static bool TryResolve(string? configured, out string url, out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(configured))
        {
            url = string.Empty;
            errorMessage =
                "Payment portal URL is not configured. " + ItContactNote;
            return false;
        }

        if (!Uri.TryCreate(configured.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            url = string.Empty;
            errorMessage =
                "Payment portal URL is invalid (must be an http/https absolute URL). " + ItContactNote;
            return false;
        }

        url = configured.Trim();
        errorMessage = null;
        return true;
    }
}
