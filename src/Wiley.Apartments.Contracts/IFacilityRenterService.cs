using Wiley.Apartments.Domain;

namespace Wiley.Apartments.Contracts;

public interface IFacilityRenterService
{
    Task<IReadOnlyList<FacilityRenter>> SearchAsync(
        string? query = null,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    Task<FacilityRenter?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<FacilityRenter> CreateAsync(FacilityRenter renter, CancellationToken cancellationToken = default);

    Task<FacilityRenter> UpdateAsync(FacilityRenter renter, CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
