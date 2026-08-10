using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Wiley.Apartments.Web.Configuration;

namespace Wiley.Apartments.Web.Services;

/// <summary>Verifies NAS DocumentRoot exists and is writable (spec edge case 2).</summary>
public sealed class DocumentRootHealthCheck : IHealthCheck
{
    private readonly ClerkSuiteOptions _options;
    private readonly IHostEnvironment _environment;

    public DocumentRootHealthCheck(IOptions<ClerkSuiteOptions> options, IHostEnvironment environment)
    {
        _options = options.Value;
        _environment = environment;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var root = ResolveRoot();
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

    private string ResolveRoot()
    {
        var root = _options.DocumentRoot;
        return Path.IsPathRooted(root)
            ? root
            : Path.GetFullPath(Path.Combine(_environment.ContentRootPath, root));
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
