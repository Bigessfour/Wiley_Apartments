using Wiley.Apartments.Domain;

namespace Wiley.Apartments.Contracts;

public interface ILeaseService
{
    Task<IReadOnlyList<LeaseTemplateInfo>> ListTemplatesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Lease>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Lease>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<Lease?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Lease> CreateDraftAsync(
        Guid unitId,
        Guid tenantId,
        string templateFileName,
        DateTime startUtc,
        DateTime endUtc,
        decimal rent,
        decimal deposit,
        CancellationToken cancellationToken = default);

    /// <summary>Fill template (prefer fillable PDF) under DocumentRoot/leases. Remains Draft until signed/activated.</summary>
    Task<Lease> GenerateDocumentsAsync(Guid leaseId, CancellationToken cancellationToken = default);

    /// <summary>Store signed PDF in the vault, link to lease, set status Active.</summary>
    Task<Lease> AttachSignedDocumentAsync(
        Guid leaseId,
        string originalFileName,
        string contentType,
        Stream content,
        string uploadedBy,
        CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
