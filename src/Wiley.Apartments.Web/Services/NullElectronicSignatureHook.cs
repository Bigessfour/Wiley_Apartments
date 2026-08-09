using Wiley.Apartments.Contracts;

namespace Wiley.Apartments.Web.Services;

/// <summary>v1 placeholder — PDF export/upload covers clerk workflow; provider integration is post-v1.</summary>
public sealed class NullElectronicSignatureHook : IElectronicSignatureHook
{
    public bool IsConfigured => false;

    public Task<ElectronicSignatureRequestResult> RequestSignatureAsync(
        Guid leaseId,
        string pdfRelativePath,
        string requestedBy,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new ElectronicSignatureRequestResult(
            Status: "NotConfigured",
            Message:
            "E-signature provider is not configured. Download/print the PDF for wet-ink signature, " +
            "or upload a signed PDF on the lease page. Hook: IElectronicSignatureHook.",
            ExternalReference: null));
    }
}
