namespace Wiley.Apartments.Web.Infrastructure;

public static class SyncfusionLicenseConfiguration
{
    public static string? ResolveLicenseKey(IConfiguration configuration)
    {
        var licenseKey = configuration["SYNCFUSION_LICENSE_KEY"]
            ?? configuration["Syncfusion:LicenseKey"];

        return string.IsNullOrWhiteSpace(licenseKey) ? null : licenseKey.Trim();
    }
}
