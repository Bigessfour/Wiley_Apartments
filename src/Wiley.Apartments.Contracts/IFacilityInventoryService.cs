using Wiley.Apartments.Domain;

namespace Wiley.Apartments.Contracts;

public interface IFacilityInventoryService
{
    Task<IReadOnlyList<FacilityInventoryItem>> ListAsync(
        Guid unitId,
        FacilityInventoryCategory? category = null,
        CancellationToken cancellationToken = default);

    Task<FacilityInventoryItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<FacilityInventoryItem> CreateAsync(
        FacilityInventoryItem item,
        CancellationToken cancellationToken = default);

    Task<FacilityInventoryItem> UpdateAsync(
        FacilityInventoryItem item,
        CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
