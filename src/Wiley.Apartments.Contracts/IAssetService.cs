using Wiley.Apartments.Domain;

namespace Wiley.Apartments.Contracts;

public interface IAssetService
{
    Task<IReadOnlyList<Asset>> GetByUnitIdAsync(Guid unitId, CancellationToken cancellationToken = default);

    Task<Asset?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Asset>> SearchBySerialAsync(string serial, CancellationToken cancellationToken = default);

    Task<Asset> CreateAsync(Asset asset, CancellationToken cancellationToken = default);

    Task<Asset> UpdateAsync(Asset asset, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
