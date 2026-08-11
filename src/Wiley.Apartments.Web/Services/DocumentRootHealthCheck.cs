using Microsoft.Extensions.Diagnostics.HealthChecks;
using Wiley.Apartments.Contracts;
using Microsoft.Extensions.Options;
using Wiley.Apartments.Web.Configuration;

namespace Wiley.Apartments.Web.Services;

/// <summary>Verifies NAS DocumentRoot exists and is writable (spec edge case 2).</summary>
public sealed class DocumentRootHealthCheck : IHealthCheck
{
    private readonly IDocumentPathResolver _paths;

    public DocumentRootHealthCheck(IDocumentPathResolver paths)
    {
        _paths = paths;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var root = _paths.GetDocumentRoot();
            Directory.CreateDirectory(root);
            if (!Directory.Exists(root))
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Document root '{root}' does not exist. Check NAS share mount."));
            }

            var probe = Path.Combine(root, $".clerksuite-health-{Guid.NewGuid():N}");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);
            return Task.FromResult(HealthCheckResult.Healthy($"Document root writable: {root}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Document root unavailable — do not report documents as saved. Contact IT / check NAS share.",
                ex));
        }
    }
}

public static class DocumentRootAvailability
{
    public static void EnsureWritable(string documentRoot)
    {
        if (!Directory.Exists(documentRoot))
        {
            throw new InvalidOperationException(
                $"Document storage is unavailable (folder '{documentRoot}' missing). " +
                "Nothing was saved. Contact IT to restore the NAS /volume1/apartments/docs share.");
        }

        try
        {
            var probe = Path.Combine(documentRoot, $".clerksuite-write-{Guid.NewGuid():N}");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Document storage is unavailable (cannot write to NAS share). " +
                "Nothing was saved. Contact IT to check the DocumentRoot mount.",
                ex);
        }
    }
}
