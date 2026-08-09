using Wiley.Apartments.Domain;

namespace Wiley.Apartments.Contracts;

public interface IFlooringService
{
    Task<IReadOnlyList<Flooring>> GetByUnitIdAsync(Guid unitId, CancellationToken cancellationToken = default);

    Task<Flooring?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Flooring> CreateAsync(Flooring flooring, CancellationToken cancellationToken = default);

    Task<Flooring> UpdateAsync(Flooring flooring, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
