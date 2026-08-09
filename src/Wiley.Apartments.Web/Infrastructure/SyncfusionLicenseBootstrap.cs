using Syncfusion.Licensing;

namespace Wiley.Apartments.Web.Infrastructure;

public static class SyncfusionLicenseBootstrap
{
    public static void RegisterFromConfiguration(
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger logger)
    {
        var licenseKey = SyncfusionLicenseConfiguration.ResolveLicenseKey(configuration);

        if (licenseKey is null)
        {
            if (environment.IsProduction())
            {
                throw new InvalidOperationException(
                    "SYNCFUSION_LICENSE_KEY is required in Production. Set via container env or user-secrets.");
            }

            logger.LogWarning(
                "Syncfusion license key not configured. Set SYNCFUSION_LICENSE_KEY via user-secrets or environment.");
            return;
        }

        SyncfusionLicenseProvider.RegisterLicense(licenseKey);
        SyncfusionLicenseProvider.ValidateLicense([Platform.Blazor], out var errorMessage);

        if (!string.IsNullOrEmpty(errorMessage))
        {
            logger.LogWarning("Syncfusion license validation reported: {Message}", errorMessage);
        }
        else
        {
            logger.LogInformation("Syncfusion Blazor license registered (length {Length}).", licenseKey.Length);
        }
    }
}
