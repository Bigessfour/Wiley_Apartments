using Wiley.Apartments.Domain;

namespace Wiley.Apartments.Contracts;

public interface ILeaseService
{
    Task<IReadOnlyList<LeaseTemplateInfo>> ListTemplatesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Lease>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Lease>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<Lease?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Active/Amended leases whose EndUtc falls within the next <paramref name="withinDays"/> (for T6.2 dashboard).</summary>
    Task<IReadOnlyList<Lease>> GetExpiringWithinAsync(
        int withinDays,
        CancellationToken cancellationToken = default);

    Task<Lease> CreateDraftAsync(
        Guid unitId,
        Guid tenantId,
        string templateFileName,
        DateTime startUtc,
        DateTime endUtc,
        decimal rent,
        decimal deposit,
        string? customClauses = null,
        CancellationToken cancellationToken = default);

    /// <summary>Fill Brookside template under DocumentRoot/leases. Remains Draft until signed. Copies rent onto the unit and starts occupancy when the unit is vacant.</summary>
    Task<Lease> GenerateDocumentsAsync(Guid leaseId, CancellationToken cancellationToken = default);

    /// <summary>Store signed PDF in the vault, link to lease, set status Active.</summary>
    Task<Lease> AttachSignedDocumentAsync(
        Guid leaseId,
        string originalFileName,
        string contentType,
        Stream content,
        string uploadedBy,
        CancellationToken cancellationToken = default);

    /// <summary>Update terms on an Active/Amended lease; status becomes Amended.</summary>
    Task<Lease> AmendAsync(
        Guid leaseId,
        decimal? rent = null,
        decimal? deposit = null,
        DateTime? endUtc = null,
        string? customClauses = null,
        string? note = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a Draft successor lease (same unit/tenant/template); mark prior lease Renewed.
    /// New start defaults to prior EndUtc; clerk regenerates/signs the successor.
    /// </summary>
    Task<Lease> RenewAsync(
        Guid leaseId,
        DateTime newEndUtc,
        decimal? rent = null,
        decimal? deposit = null,
        string? note = null,
        CancellationToken cancellationToken = default);

    /// <summary>End an Active/Amended lease; status Terminated; EndUtc set to effective date when earlier.</summary>
    Task<Lease> TerminateAsync(
        Guid leaseId,
        DateTime effectiveEndUtc,
        string? note = null,
        CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
