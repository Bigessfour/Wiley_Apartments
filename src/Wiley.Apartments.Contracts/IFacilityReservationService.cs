using Wiley.Apartments.Domain;

namespace Wiley.Apartments.Contracts;

public interface IFacilityReservationService
{
    Task<IReadOnlyList<FacilityReservation>> ListAsync(
        Guid? unitId = null,
        FacilityReservationStatus? status = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        Guid? facilityRenterId = null,
        CancellationToken cancellationToken = default);

    Task<FacilityReservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<FacilityReservation> CreateAsync(
        FacilityReservation reservation,
        CancellationToken cancellationToken = default);

    Task<FacilityReservation> UpdateAsync(
        FacilityReservation reservation,
        CancellationToken cancellationToken = default);

    Task<FacilityReservation> SetStatusAsync(
        Guid id,
        FacilityReservationStatus status,
        string? note = null,
        CancellationToken cancellationToken = default);

    Task EnsureNoConfirmedOverlapAsync(
        Guid unitId,
        DateTime startUtc,
        DateTime endUtc,
        Guid? excludeId = null,
        FacilitySpace space = FacilitySpace.WholeBuilding,
        CancellationToken cancellationToken = default);
}
