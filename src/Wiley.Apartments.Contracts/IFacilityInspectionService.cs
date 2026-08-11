using Wiley.Apartments.Domain;

namespace Wiley.Apartments.Contracts;

public interface IFacilityInspectionService
{
    Task<IReadOnlyList<FacilityInspection>> ListForReservationAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FacilityInspection>> ListRecentAsync(
        int take = 50,
        CancellationToken cancellationToken = default);

    Task<FacilityInspection> CreateAsync(
        FacilityInspection inspection,
        CancellationToken cancellationToken = default);

    Task<FacilityInspection> UpdateAsync(
        FacilityInspection inspection,
        CancellationToken cancellationToken = default);
}
