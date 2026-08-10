using Wiley.Apartments.Domain;

namespace Wiley.Apartments.Contracts;

public interface IUnitService
{
    Task<IReadOnlyList<Unit>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Unit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Community Center (or first facility) unit, or null if not seeded yet.</summary>
    Task<Unit?> GetFacilityAsync(CancellationToken cancellationToken = default);

    Task<Unit> CreateAsync(Unit unit, CancellationToken cancellationToken = default);

    Task<Unit> UpdateAsync(Unit unit, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Residential unit count only (!IsFacility).</summary>
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    int MaxUnits { get; }
}
