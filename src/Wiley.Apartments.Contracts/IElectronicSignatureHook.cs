namespace Wiley.Apartments.Contracts;

/// <summary>
/// FR-012 e-signature readiness. v1 ships a no-op implementation; wire a provider later without changing lease PDF export.
/// </summary>
public interface IElectronicSignatureHook
{
    /// <summary>True when a real provider is configured (v1 Null hook returns false).</summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Request signature for an already-exported PDF under DocumentRoot.
    /// Null hook completes immediately with NotConfigured status.
    /// </summary>
    Task<ElectronicSignatureRequestResult> RequestSignatureAsync(
        Guid leaseId,
        string pdfRelativePath,
        string requestedBy,
        CancellationToken cancellationToken = default);
}

public sealed record ElectronicSignatureRequestResult(
    string Status,
    string Message,
    string? ExternalReference = null);
