using Wiley.Apartments.Domain;

namespace Wiley.Apartments.Contracts;

public interface ITenantService
{
    Task<IReadOnlyList<Tenant>> SearchAsync(
        string? query = null,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Tenant> CreateAsync(Tenant tenant, CancellationToken cancellationToken = default);

    Task<Tenant> UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<HouseholdMember> AddHouseholdMemberAsync(
        Guid tenantId,
        HouseholdMember member,
        CancellationToken cancellationToken = default);

    Task<HouseholdMember> UpdateHouseholdMemberAsync(
        HouseholdMember member,
        CancellationToken cancellationToken = default);

    Task RemoveHouseholdMemberAsync(Guid memberId, CancellationToken cancellationToken = default);

    Task<Vehicle> AddVehicleAsync(
        Guid tenantId,
        Vehicle vehicle,
        CancellationToken cancellationToken = default);

    Task<Vehicle> UpdateVehicleAsync(
        Vehicle vehicle,
        CancellationToken cancellationToken = default);

    Task RemoveVehicleAsync(Guid vehicleId, CancellationToken cancellationToken = default);

    Task<Pet> AddPetAsync(
        Guid tenantId,
        Pet pet,
        CancellationToken cancellationToken = default);

    Task<Pet> UpdatePetAsync(
        Pet pet,
        CancellationToken cancellationToken = default);

    Task RemovePetAsync(Guid petId, CancellationToken cancellationToken = default);
}
