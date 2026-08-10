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

        // Blazor UI key alone is not enough — lease PDF generation uses DocIO/PDF.
        Platform[] required =
        [
            Platform.Blazor,
            Platform.PDF,
            Platform.Word,
            Platform.WordToPDF,
            Platform.PDFViewer
        ];
        SyncfusionLicenseProvider.ValidateLicense(required, out var errorMessage);

        if (!string.IsNullOrEmpty(errorMessage))
        {
            logger.LogWarning(
                "Syncfusion license validation reported: {Message}. " +
                "Lease PDFs may include a trial watermark until SYNCFUSION_LICENSE_KEY covers PDF/Word/PDFViewer.",
                errorMessage);
        }
        else
        {
            logger.LogInformation(
                "Syncfusion license registered for Blazor + PDF/Word (key length {Length}).",
                licenseKey.Length);
        }
    }
}
