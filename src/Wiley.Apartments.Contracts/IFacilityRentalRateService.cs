using Wiley.Apartments.Domain;

namespace Wiley.Apartments.Contracts;

public interface IFacilityRentalRateService
{
    Task<IReadOnlyList<FacilityRentalRate>> ListAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    Task<FacilityRentalRate> UpsertAsync(
        FacilityRentalRate rate,
        CancellationToken cancellationToken = default);
}
