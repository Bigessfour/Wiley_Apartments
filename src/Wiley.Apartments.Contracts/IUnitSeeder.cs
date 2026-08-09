using Wiley.Apartments.Domain;

namespace Wiley.Apartments.Contracts;

public interface IUnitSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
