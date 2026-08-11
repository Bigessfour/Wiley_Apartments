using Wiley.Apartments.Domain;

namespace Wiley.Apartments.Contracts;

public interface ILateFeeSettingsService
{
    Task<LateFeeSettings> GetAsync(CancellationToken cancellationToken = default);

    Task<LateFeeSettings> UpdateAsync(
        bool enabled,
        decimal amount,
        int graceDays,
        CancellationToken cancellationToken = default);
}
