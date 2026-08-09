namespace Wiley.Apartments.Web.Infrastructure;

public static class LoginRedirectHelper
{
    /// <summary>
    /// Builds a safe return path from <see cref="Microsoft.AspNetCore.Components.NavigationManager.ToBaseRelativePath"/>,
    /// which omits the leading slash (e.g. <c>units</c> → <c>/units</c>).
    /// </summary>
    public static string FromBaseRelativePath(string? baseRelativePath)
    {
        if (string.IsNullOrWhiteSpace(baseRelativePath))
        {
            return "/";
        }

        var trimmed = baseRelativePath.Trim();

        // Do not "rescue" absolute / scheme-relative values by prefixing '/'.
        if (Uri.IsWellFormedUriString(trimmed, UriKind.Absolute)
            || trimmed.StartsWith("//", StringComparison.Ordinal)
            || trimmed.StartsWith("/\\", StringComparison.Ordinal)
            || trimmed.StartsWith('\\'))
        {
            return "/";
        }

        var withRoot = trimmed.StartsWith('/') ? trimmed : "/" + trimmed;
        return GetSafeReturnUrl(withRoot);
    }

    /// <summary>
    /// Returns a same-origin relative path safe for post-login redirect.
    /// Rejects absolute URLs and protocol-relative paths (open-redirect defenses).
    /// </summary>
    public static string GetSafeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return "/";
        }

        if (Uri.IsWellFormedUriString(returnUrl, UriKind.Absolute))
        {
            return "/";
        }

        if (!returnUrl.StartsWith('/'))
        {
            return "/";
        }

        if (returnUrl.StartsWith("//", StringComparison.Ordinal)
            || returnUrl.StartsWith("/\\", StringComparison.Ordinal))
        {
            return "/";
        }

        return returnUrl;
    }
}
