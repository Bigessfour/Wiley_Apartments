using Wiley.Apartments.Domain;

namespace Wiley.Apartments.Contracts;

public interface IUnitService
{
    Task<IReadOnlyList<Unit>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Unit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Unit> CreateAsync(Unit unit, CancellationToken cancellationToken = default);

    Task<Unit> UpdateAsync(Unit unit, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    int MaxUnits { get; }
}
