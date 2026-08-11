using Wiley.Apartments.Domain;

namespace Wiley.Apartments.Contracts;

public interface IAuditQueryService
{
    Task<IReadOnlyList<AuditLog>> QueryAsync(
        string? entityType = null,
        string? userId = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        int take = 200,
        CancellationToken cancellationToken = default);
}
