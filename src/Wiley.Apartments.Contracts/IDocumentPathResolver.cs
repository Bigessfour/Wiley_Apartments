namespace Wiley.Apartments.Contracts;

/// <summary>Resolves NAS document root (env default + optional clerk override from Settings).</summary>
public interface IDocumentPathResolver
{
    /// <summary>Effective absolute document root path.</summary>
    Task<string> GetDocumentRootAsync(CancellationToken cancellationToken = default);

    /// <summary>Synchronous resolve for health checks / controllers (uses cache or options fallback).</summary>
    string GetDocumentRoot();

    Task SetDocumentRootAsync(string absoluteOrRelativePath, string? updatedBy, CancellationToken cancellationToken = default);

    /// <summary>Configured default from appsettings / env (before clerk override).</summary>
    string ConfiguredDefaultRoot { get; }
}
