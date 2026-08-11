using Wiley.Apartments.Domain;

namespace Wiley.Apartments.Contracts;

public interface IOccupancyService
{
    Task<Occupancy?> GetCurrentForUnitAsync(Guid unitId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Occupancy>> GetHistoryForUnitAsync(
        Guid unitId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Occupancy>> GetHistoryForTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<Occupancy> StartAsync(
        Guid unitId,
        Guid tenantId,
        DateTime? startUtc = null,
        CancellationToken cancellationToken = default);

    Task<Occupancy> EndAsync(
        Guid unitId,
        DateTime? endUtc = null,
        CancellationToken cancellationToken = default);
}
