using Wiley.Apartments.Domain;

namespace Wiley.Apartments.Contracts;

public interface IMaintenanceService
{
    Task<MaintenanceRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MaintenanceRequest>> GetAllAsync(
        bool openOnly = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MaintenanceRequest>> GetForUnitAsync(
        Guid unitId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MaintenanceRequest>> GetForAssetAsync(
        Guid assetId,
        CancellationToken cancellationToken = default);

    Task<MaintenanceRequest> CreateAsync(
        Guid unitId,
        string description,
        MaintenancePriority priority = MaintenancePriority.Normal,
        Guid? assetId = null,
        string? notes = null,
        CancellationToken cancellationToken = default);

    Task<MaintenanceRequest> UpdateAsync(
        Guid id,
        string description,
        MaintenanceStatus status,
        MaintenancePriority priority,
        Guid? assetId = null,
        decimal? cost = null,
        string? notes = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks completed; when <paramref name="cost"/> &gt; 0, posts a Repair <see cref="UnitOperatingCost"/> linked to this WO.
    /// </summary>
    Task<MaintenanceRequest> CompleteAsync(
        Guid id,
        decimal? cost = null,
        DateTime? completedUtc = null,
        string? notes = null,
        bool postOperatingCost = true,
        CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
